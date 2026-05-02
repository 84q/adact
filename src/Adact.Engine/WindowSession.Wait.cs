using System.Diagnostics;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;

namespace Adact.Engine;

public sealed partial class WindowSession
{
    /// <summary>wait-for の内部ポーリング間隔 (設計 022 §13)。</summary>
    private static readonly TimeSpan WaitForPollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// 指定 element ref の状態を満たすまで待機する (設計 022 §6 / §7)。
    /// auto-snapshot は発火しない。各ポーリング反復で内部 snapshot を取り直し、
    /// stableKey ベースで再解決を試みる。
    /// </summary>
    /// <param name="refId">対象 element ref。</param>
    /// <param name="state">満たすべき状態。</param>
    /// <param name="timeout">待機タイムアウト。<see cref="TimeSpan.Zero"/> 以下は <see cref="ArgumentOutOfRangeException"/>。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>満たされた状態と ref を含む <see cref="WaitForResult"/>。</returns>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="refId"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> が 0 以下。</exception>
    /// <exception cref="WaitTimeoutException">タイムアウト内に状態を満たせなかった。</exception>
    public Task<WaitForResult> WaitForRefAsync(
        string refId,
        WaitForState state,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(refId);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be positive.");
        ct.ThrowIfCancellationRequested();

        return PollAsync(
            timeout,
            attempt: c =>
            {
                // 各反復で snapshot を取り直し、registry の現スナップショット mapping を更新する。
                _ = TakeInternalSnapshot(c);
                IElement? el = null;
                try
                {
                    el = _registry.Resolve(refId);
                }
                catch (RefNotFoundException)
                {
                    el = null;
                }

                if (el is null)
                {
                    return state == WaitForState.Detached
                        ? new WaitForResult(refId, WaitForState.Detached)
                        : null;
                }

                return CheckRefState(refId, el, state);
            },
            timeoutMessage: $"wait-for did not observe state '{WaitForStateParser.ToWireString(state)}' for ref '{refId}' within {(int)timeout.TotalMilliseconds}ms.",
            ct);
    }

    /// <summary>
    /// 検索条件モードの wait-for (設計 022 §7)。snapshot を内部リトライしながら一致要素の出現を待つ。
    /// </summary>
    /// <param name="query">検索条件。少なくとも 1 フィールド必須。</param>
    /// <param name="state">満たすべき状態。<see cref="WaitForState.Detached"/> はサポートしない。</param>
    /// <param name="timeout">待機タイムアウト。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>見つかった要素の ref と最終状態を含む <see cref="WaitForResult"/>。</returns>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> が null。</exception>
    /// <exception cref="ArgumentException">クエリが空、または state が <see cref="WaitForState.Detached"/>。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> が 0 以下。</exception>
    /// <exception cref="WaitTimeoutException">タイムアウト内に一致要素を観測できなかった。</exception>
    public Task<WaitForResult> WaitForQueryAsync(
        WaitForElementQuery query,
        WaitForState state,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query);
        if (!query.HasAnyCondition)
            throw new ArgumentException("Query must specify at least one condition.", nameof(query));
        if (state == WaitForState.Detached)
            throw new ArgumentException("'detached' state is not supported in query mode (no ref to track).", nameof(state));
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be positive.");
        ct.ThrowIfCancellationRequested();

        return PollAsync(
            timeout,
            attempt: c =>
            {
                _ = TakeInternalSnapshot(c);
                foreach (var (foundRef, el) in _registry.EnumerateCurrent())
                {
                    if (!QueryMatches(query, el)) continue;
                    var hit = CheckRefState(foundRef, el, state);
                    if (hit is not null) return hit;
                }
                return null;
            },
            timeoutMessage: $"wait-for did not observe a matching element for state '{WaitForStateParser.ToWireString(state)}' within {(int)timeout.TotalMilliseconds}ms.",
            ct);
    }

    /// <summary>
    /// 指定 IElement が <paramref name="state"/> を満たすなら <see cref="WaitForResult"/> を返す。満たさなければ null。
    /// </summary>
    /// <param name="refId">結果に詰める ref。</param>
    /// <param name="el">判定対象要素。</param>
    /// <param name="state">期待状態。</param>
    /// <returns>状態を満たすなら <see cref="WaitForResult"/>、未達なら null。</returns>
    private static WaitForResult? CheckRefState(string refId, IElement el, WaitForState state)
    {
        return state switch
        {
            WaitForState.Attached => new WaitForResult(refId, WaitForState.Attached),
            WaitForState.Visible => !el.IsOffscreen ? new WaitForResult(refId, WaitForState.Visible) : null,
            WaitForState.Hidden => el.IsOffscreen ? new WaitForResult(refId, WaitForState.Hidden) : null,
            WaitForState.Enabled => el.IsEnabled ? new WaitForResult(refId, WaitForState.Enabled) : null,
            WaitForState.Disabled => !el.IsEnabled ? new WaitForResult(refId, WaitForState.Disabled) : null,
            // detached: 要素が見つかった以上は未達。呼び出し側で判定する。
            WaitForState.Detached => null,
            _ => null,
        };
    }

    /// <summary>クエリ条件がすべて要素にマッチするか判定する (case-insensitive 完全一致)。</summary>
    /// <param name="query">検索条件。</param>
    /// <param name="el">対象要素。</param>
    /// <returns>すべての設定済みフィールドがマッチすれば true。</returns>
    private static bool QueryMatches(WaitForElementQuery query, IElement el)
    {
        if (!string.IsNullOrEmpty(query.Name)
            && !string.Equals(query.Name, el.Name, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(query.ControlType)
            && !string.Equals(query.ControlType, el.ControlType, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(query.AutomationId)
            && !string.Equals(query.AutomationId, el.AutomationId, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(query.ClassName)
            && !string.Equals(query.ClassName, el.ClassName, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    /// <summary>
    /// 共通ポーリング。<paramref name="attempt"/> が非 null を返したら成功で打ち切り、
    /// null を返した場合は <see cref="WaitForPollInterval"/> 待機して再試行する。
    /// 全体のタイムアウトに達したら <see cref="WaitTimeoutException"/>。
    /// </summary>
    /// <typeparam name="T">成功結果の型。</typeparam>
    /// <param name="timeout">全体タイムアウト。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <param name="attempt">1 回分の試行。成功時に非 null を返す。例外でループは中断する。</param>
    /// <param name="timeoutMessage">タイムアウト時のメッセージ。</param>
    /// <returns>成功結果。</returns>
    private async Task<T> PollAsync<T>(
        TimeSpan timeout,
        Func<CancellationToken, T?> attempt,
        string timeoutMessage,
        CancellationToken ct)
        where T : class
    {
        // 1 回分の試行は UIA 操作を含むため、毎回 gate を取得しなおす。
        // 長時間の待機中に他 session の操作を完全にブロックしないため、各反復で release する。
        var deadline = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var result = await RunSerializedAsync(c =>
            {
                c.ThrowIfCancellationRequested();
                return Task.FromResult(attempt(c));
            }, ct).ConfigureAwait(false);

            if (result is not null) return result;

            if (Stopwatch.GetTimestamp() >= deadline)
                throw new WaitTimeoutException(timeoutMessage);

            try { await Task.Delay(WaitForPollInterval, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { throw; }
        }
    }

    /// <summary>
    /// gate 内で snapshot を取り、registry の現スナップショット mapping を更新する内部用 snapshot 取得。
    /// 外部 <see cref="SnapshotAsync"/> と異なり、結果の JSON 文字列だけを返す (多くの呼び出し側は破棄する)。
    /// </summary>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>snapshot 結果。</returns>
    private SnapshotResult TakeInternalSnapshot(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var modals = DetectModalElements();
        var popups = DetectPopupElements(modals);
        var now = DateTimeOffset.UtcNow;
        var input = new SnapshotBuildInput(
            _rootElement, modals, popups, new SnapshotOptions(),
            WindowTitle: Title,
            ProcessName: ProcessName,
            ProcessId: ProcessId,
            GeneratedAt: now);
        try
        {
            var built = new SnapshotBuilder(_registry).Build(input);
            return new SnapshotResult(
                Json: built.Json,
                SessionId: built.SessionId,
                WindowTitle: Title,
                ProcessName: ProcessName,
                ProcessId: ProcessId,
                GeneratedAt: now);
        }
        catch (Exception ex) when (ex is not AdactException)
        {
            throw new SnapshotException("Snapshot construction failed.", ex);
        }
    }
}
