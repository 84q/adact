using System.Collections.Concurrent;
using Adact.Engine;
using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adact.Mcp.Stdio;

/// <summary>
/// MCP 層が保持する Session ストア。複数 <see cref="WindowSession"/> を sessionId (例: "s1") で管理し、
/// 最後にアタッチした Session を「アクティブ Session」として保持する。
/// UIA は同時呼び出しに弱いため <see cref="SemaphoreSlim"/> でツール呼び出しを直列化する。
/// </summary>
public sealed class SessionStore : IDisposable
{
  private readonly UiaEngine _engine;
  private readonly ILogger<SessionStore> _logger;
  private readonly ConcurrentDictionary<string, WindowSession> _sessions = new();
  private readonly SemaphoreSlim _lock = new(1, 1);
  private string? _activeSessionId;
  private bool _disposed;

  public SessionStore(UiaEngine engine, ILogger<SessionStore>? logger = null)
  {
    _engine = engine;
    _logger = logger ?? NullLogger<SessionStore>.Instance;
  }

  public UiaEngine Engine => _engine;
  public string? ActiveSessionId => _activeSessionId;

  /// <summary>すべての MCP ツール呼び出しはこの guard を取得する (UIA 直列化)。</summary>
  public Task<IDisposable> AcquireAsync(CancellationToken ct)
      => SemaphoreGuard.AcquireAsync(_lock, ct);

  public void Register(WindowSession session)
  {
    var id = $"s{session.SessionId}";
    _sessions[id] = session;
    _activeSessionId = id;
  }

  public bool TryGet(string sessionId, out WindowSession session)
  {
    if (_sessions.TryGetValue(sessionId, out var s))
    {
      session = s;
      return true;
    }
    session = null!;
    return false;
  }

  public WindowSession? GetActiveOrNull()
  {
    if (_activeSessionId is null) return null;
    return _sessions.TryGetValue(_activeSessionId, out var s) ? s : null;
  }

  /// <summary>Ref ID から sid を抽出し、対応する Session を返す。失敗時は null。</summary>
  public WindowSession? ResolveByRef(string refId)
  {
    if (!RefId.TryParse(refId, out var sid, out _, out _)) return null;
    var key = $"s{sid}";
    return _sessions.TryGetValue(key, out var s) ? s : null;
  }

  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;
    foreach (var s in _sessions.Values)
    {
      try { s.Dispose(); } catch (Exception ex) { _logger.LogDebug(ex, "Disposing session failed"); }
    }
    _sessions.Clear();
    try { _engine.Dispose(); } catch { }
    _lock.Dispose();
  }

  private sealed class SemaphoreGuard : IDisposable
  {
    private readonly SemaphoreSlim _sem;
    private bool _released;
    private SemaphoreGuard(SemaphoreSlim sem) { _sem = sem; }
    public static async Task<IDisposable> AcquireAsync(SemaphoreSlim sem, CancellationToken ct)
    {
      await sem.WaitAsync(ct).ConfigureAwait(false);
      return new SemaphoreGuard(sem);
    }
    public void Dispose()
    {
      if (_released) return;
      _released = true;
      try { _sem.Release(); } catch { }
    }
  }
}
