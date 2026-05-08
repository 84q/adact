using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Adact.Engine;
using Adact.Engine.Snapshot;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adact.Mcp.Common;

/// <summary>
/// MCP 層が保持する Session ストア。複数 <see cref="IWindowSession"/> を sessionId (例: "s1") で管理し、
/// 最後にアタッチした Session を「アクティブ Session」として保持する。
/// UIA は同時呼び出しに弱いため <see cref="SemaphoreSlim"/> でツール呼び出しを直列化する。
/// </summary>
public sealed class SessionStore : IDisposable
{
    /// <summary>attach や list の実行主体となる UIA エンジン。ストアは利用するが所有しない。</summary>
    private readonly UiaEngine _engine;
    /// <summary>Dispose / 例外トレース用のロガー。</summary>
    private readonly ILogger<SessionStore> _logger;
    /// <summary>sessionId (例: <c>s1</c>) → <see cref="IWindowSession"/> の辞書。</summary>
    private readonly ConcurrentDictionary<string, IWindowSession> _sessions = new();
    /// <summary>UIA 呼び出しを直列化するための semaphore (初期カウント 1)。</summary>
    private readonly SemaphoreSlim _lock = new(1, 1);
    /// <summary>最後に attach / register された session の ID。存在しなければ <c>null</c>。</summary>
    private string? _activeSessionId;
    /// <summary><see cref="Dispose"/> が二重呼び出されるのを防ぐフラグ。</summary>
    private int _disposed;

    /// <summary>
    /// 新しい <see cref="SessionStore"/> を構築する。
    /// </summary>
    /// <param name="engine">起動済みの <see cref="UiaEngine"/>。ライフサイクル管理は呼び出し側 / DI が担う。</param>
    /// <param name="logger">Dispose 時の未マップ例外ロガー。<c>null</c> の場合は <see cref="NullLogger{T}"/>。</param>
    public SessionStore(UiaEngine engine, ILogger<SessionStore>? logger = null)
    {
        _engine = engine;
        _logger = logger ?? NullLogger<SessionStore>.Instance;
    }

    /// <summary>ストアが使用している <see cref="UiaEngine"/>。MCP ツールが list / attach を呼ぶ際に利用する。</summary>
    public UiaEngine Engine => _engine;
    /// <summary>現在のアクティブ session ID。一度も attach していないか、すべて detach 済みの場合は <c>null</c>。</summary>
    public string? ActiveSessionId => Volatile.Read(ref _activeSessionId);

    /// <summary>すべての MCP ツール呼び出しはこの guard を取得する (UIA 直列化)。</summary>
    /// <param name="ct">キャンセル トークン。</param>
    /// <returns>取得中はこれを dispose するまでロックを保持する <see cref="IDisposable"/>。</returns>
    public Task<IDisposable> AcquireAsync(CancellationToken ct)
        => SemaphoreGuard.AcquireAsync(_lock, _logger, ct);

    /// <summary>
    /// attach 成功時に session をストアへ登録し、同時にアクティブ session とする。
    /// 同じ sessionId のエントリがあれば上書きされる。
    /// </summary>
    /// <param name="session">登録する <see cref="IWindowSession"/>。</param>
    public void Register(IWindowSession session)
    {
        var id = $"s{session.SessionId}";
        _sessions[id] = session;
        Volatile.Write(ref _activeSessionId, id);
    }

    /// <summary>
    /// <paramref name="sessionId"/> に一致する session を取得する。
    /// </summary>
    /// <param name="sessionId">探す sessionId (例: <c>s1</c>)。</param>
    /// <param name="session">見つかった session。見つからない場合は <c>null!</c>。</param>
    /// <returns>見つかったかどうか。</returns>
    public bool TryGet(string sessionId, out IWindowSession session)
    {
        if (_sessions.TryGetValue(sessionId, out var s))
        {
            session = s;
            return true;
        }
        session = null!;
        return false;
    }

    /// <summary>アクティブ session を返す。存在しない (未 attach / detach 済み) なら <c>null</c>。</summary>
    /// <returns>アクティブ session、または <c>null</c>。</returns>
    public IWindowSession? GetActiveOrNull()
    {
        var activeId = Volatile.Read(ref _activeSessionId);
        if (activeId is null) return null;
        return _sessions.TryGetValue(activeId, out var s) ? s : null;
    }

    /// <summary>Ref ID から sid を抽出し、対応する Session を返す。失敗時は null。</summary>
    /// <param name="refId"><c>s&lt;sid&gt;e&lt;eid&gt;</c> 形式の element ref。</param>
    /// <returns>解決できた session、または <c>null</c>。</returns>
    public IWindowSession? ResolveByRef(string refId)
    {
        if (!RefId.TryParse(refId, out var sid, out _)) return null;
        var key = $"s{sid}";
        return _sessions.TryGetValue(key, out var s) ? s : null;
    }

    /// <summary>
    /// dictionary から該当 sessionId を削除する。<paramref name="sessionId"/> が active session の場合は
    /// active を null に戻す。Session の Dispose は呼び出し側で行う。
    /// </summary>
    /// <param name="sessionId">削除する session の ID。</param>
    /// <param name="session">削除された session。見つからない場合は <c>null</c>。</param>
    /// <returns>削除できたかどうか。</returns>
    public bool TryRemove(string sessionId, [NotNullWhen(true)] out IWindowSession? session)
    {
        if (_sessions.TryRemove(sessionId, out var removed))
        {
            if (string.Equals(Volatile.Read(ref _activeSessionId), sessionId, StringComparison.Ordinal))
            {
                Volatile.Write(ref _activeSessionId, null);
            }
            session = removed;
            return true;
        }
        session = null;
        return false;
    }

    /// <summary>現在保持しているすべての (sessionId, IWindowSession) のスナップショット。</summary>
    /// <returns>スナップショット時点のエントリ一覧。後続の変更は反映されない。</returns>
    public IReadOnlyList<KeyValuePair<string, IWindowSession>> ListAll()
    {
        return _sessions.ToArray();
    }

    /// <summary>
    /// 保持しているすべての <see cref="IWindowSession"/> と semaphore を解放する。
    /// </summary>
    /// <remarks>
    /// 二重呼び出しは無視される。Dispose 中に起きた個別の例外は握りつぶしてデバッグログに記録し、ループを継続する。
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;
        foreach (var s in _sessions.Values)
        {
            try { s.Dispose(); } catch (Exception ex) { _logger.LogDebug(ex, "Disposing session failed"); }
        }
        _sessions.Clear();
        _lock.Dispose();
    }

    /// <summary>
    /// <see cref="SemaphoreSlim"/> を await で安全に取得・解放するためのインターナル guard。
    /// </summary>
    private sealed class SemaphoreGuard : IDisposable
    {
        /// <summary>guard が保持している semaphore。</summary>
        private readonly SemaphoreSlim _sem;
        /// <summary>診断用ロガー。</summary>
        private readonly ILogger _logger;
        /// <summary>二重 Release を防止するためのフラグ。</summary>
        private bool _released;
        /// <summary><see cref="AcquireAsync"/> からしか生成されないよう private。</summary>
        /// <param name="sem">取得済みの semaphore。</param>
        /// <param name="logger">診断用ロガー。</param>
        private SemaphoreGuard(SemaphoreSlim sem, ILogger logger) { _sem = sem; _logger = logger; }
        /// <summary>
        /// semaphore を 1 スロット取得し、解放用の <see cref="IDisposable"/> を返す。
        /// </summary>
        /// <param name="sem">取得対象の semaphore。</param>
        /// <param name="logger">診断用ロガー。</param>
        /// <param name="ct">キャンセル トークン。</param>
        /// <returns>Dispose 時に semaphore を release する guard。</returns>
        public static async Task<IDisposable> AcquireAsync(SemaphoreSlim sem, ILogger logger, CancellationToken ct)
        {
            await sem.WaitAsync(ct).ConfigureAwait(false);
            return new SemaphoreGuard(sem, logger);
        }
        /// <summary>semaphore を release する。二重呼び出しは無視される。</summary>
        public void Dispose()
        {
            if (_released) return;
            _released = true;
            try { _sem.Release(); } catch (Exception ex) { _logger.LogTrace(ex, "Semaphore release failed"); }
        }
    }
}
