using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Adact.Engine;
using Adact.Engine.Snapshot;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Adact.Mcp.Common;

/// <summary>
/// Stores live window sessions and serializes access to the engine.
/// </summary>
public sealed class SessionStore : IDisposable
{
    private readonly UiaEngine _engine;
    private readonly ILogger<SessionStore> _logger;
    private readonly ConcurrentDictionary<string, IWindowSession> _sessions = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _activeSessionId;
    private int _disposed;

    /// <summary>
    /// Creates a new session store.
    /// </summary>
    public SessionStore(UiaEngine engine, ILogger<SessionStore>? logger = null)
    {
        _engine = engine;
        _logger = logger ?? NullLogger<SessionStore>.Instance;
    }

    /// <summary>
    /// Gets the engine used to create and attach sessions.
    /// </summary>
    public UiaEngine Engine => _engine;

    /// <summary>
    /// Gets the active session identifier, if any.
    /// </summary>
    public string? ActiveSessionId => Volatile.Read(ref _activeSessionId);

    /// <summary>
    /// Acquires the session store lock.
    /// </summary>
    public Task<IDisposable> AcquireAsync(CancellationToken ct)
        => SemaphoreGuard.AcquireAsync(_lock, _logger, ct);

    /// <summary>
    /// Registers a live session and marks it as active.
    /// </summary>
    public void Register(IWindowSession session)
    {
        var id = $"s{session.SessionId}";
        _sessions[id] = session;
        Volatile.Write(ref _activeSessionId, id);
    }

    /// <summary>
    /// Tries to get a session by its session ID.
    /// </summary>
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

    /// <summary>
    /// Gets the active session, or <see langword="null"/> when none is active.
    /// </summary>
    public IWindowSession? GetActiveOrNull()
    {
        var activeId = Volatile.Read(ref _activeSessionId);
        if (activeId is null) return null;
        return _sessions.TryGetValue(activeId, out var s) ? s : null;
    }

    /// <summary>
    /// Resolves a session from an element ref ID.
    /// </summary>
    public IWindowSession? ResolveByRef(string refId)
    {
        if (!RefId.TryParse(refId, out var sid, out _)) return null;
        var key = $"s{sid}";
        return _sessions.TryGetValue(key, out var s) ? s : null;
    }

    /// <summary>
    /// Removes a session from the store.
    /// </summary>
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

    /// <summary>
    /// Lists all known sessions.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, IWindowSession>> ListAll()
    {
        return _sessions.ToArray();
    }

    /// <summary>
    /// Disposes all tracked sessions and releases the store lock.
    /// </summary>
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
    /// </summary>
    private sealed class SemaphoreGuard : IDisposable
    {
        private readonly SemaphoreSlim _sem;
        private readonly ILogger _logger;
        private bool _released;
        private SemaphoreGuard(SemaphoreSlim sem, ILogger logger) { _sem = sem; _logger = logger; }
        /// <summary>
        /// Acquires the semaphore and returns a disposable releaser.
        /// </summary>
        public static async Task<IDisposable> AcquireAsync(SemaphoreSlim sem, ILogger logger, CancellationToken ct)
        {
            await sem.WaitAsync(ct).ConfigureAwait(false);
            return new SemaphoreGuard(sem, logger);
        }
        public void Dispose()
        {
            if (_released) return;
            _released = true;
            try { _sem.Release(); } catch (Exception ex) { _logger.LogTrace(ex, "Semaphore release failed"); }
        }
    }
}
