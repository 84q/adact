using System.Globalization;

using Adact.Engine;

namespace Adact.Mcp.Common;

/// <summary>
/// Assigns and tracks stable window refs for top-level windows.
/// </summary>
public sealed class WindowRefStore
{
    private static readonly TimeSpan DefaultRetiredEntryTtl = TimeSpan.FromMinutes(5);

    private readonly object _lock = new();
    private readonly Dictionary<WindowKey, WindowRefEntry> _entries = new();
    private int _nextRef;
    private readonly TimeSpan _retiredEntryTtl;
    private readonly Func<DateTimeOffset> _utcNow;

    /// <summary>
    /// Creates a new window reference store.
    /// </summary>
    public WindowRefStore(TimeSpan? retiredEntryTtl = null, Func<DateTimeOffset>? utcNow = null)
    {
        _retiredEntryTtl = retiredEntryTtl ?? DefaultRetiredEntryTtl;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Updates or assigns a window reference for the specified window.
    /// </summary>
    public WindowRefEntry SyncOrAssign(WindowKey key, WindowInfo info)
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());

            if (_entries.TryGetValue(key, out var existing))
            {
                if (existing.Retired)
                {
                    var revived = existing with { Info = info, SessionId = null, Retired = false, RetiredAtUtc = null };
                    _entries[key] = revived;
                    return revived;
                }

                var updated = existing with { Info = info };
                _entries[key] = updated;
                return updated;
            }

            var n = ++_nextRef;
            var windowRef = "w" + n.ToString(CultureInfo.InvariantCulture);
            var entry = new WindowRefEntry(windowRef, key, info, SessionId: null, Retired: false);
            _entries[key] = entry;
            return entry;
        }
    }

    /// <summary>
    /// Marks missing windows as retired.
    /// </summary>
    public void RetireMissing(IEnumerable<WindowKey> presentKeys)
    {
        ArgumentNullException.ThrowIfNull(presentKeys);

        var present = new HashSet<WindowKey>(presentKeys);
        lock (_lock)
        {
            var now = _utcNow();
            PurgeExpiredRetiredEntriesCore(now);

            List<WindowKey>? toRetire = null;
            foreach (var kv in _entries)
            {
                if (kv.Value.Retired) continue;
                if (present.Contains(kv.Key)) continue;
                (toRetire ??= new List<WindowKey>()).Add(kv.Key);
            }
            if (toRetire is not null)
            {
                foreach (var key in toRetire)
                {
                    _entries[key] = _entries[key] with { SessionId = null, Retired = true, RetiredAtUtc = now };
                }
            }
        }
    }

    /// <summary>
    /// Tries to resolve a window reference.
    /// </summary>
    public bool TryResolve(string windowRef, out WindowRefEntry entry)
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            foreach (var e in _entries.Values)
            {
                if (e.Retired) continue;
                if (string.Equals(e.WindowRef, windowRef, StringComparison.Ordinal))
                {
                    entry = e;
                    return true;
                }
            }
        }
        entry = default!;
        return false;
    }

    /// <summary>
    /// Associates a live session with a window reference.
    /// </summary>
    public void AssociateSession(string windowRef, string sessionId)
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            foreach (var kv in _entries)
            {
                if (kv.Value.Retired) continue;
                if (!string.Equals(kv.Value.WindowRef, windowRef, StringComparison.Ordinal)) continue;
                _entries[kv.Key] = kv.Value with { SessionId = sessionId };
                return;
            }
        }
    }

    /// <summary>
    /// Clears the session association for a window reference.
    /// </summary>
    public void ClearSession(string windowRef)
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            foreach (var kv in _entries)
            {
                if (!string.Equals(kv.Value.WindowRef, windowRef, StringComparison.Ordinal)) continue;
                _entries[kv.Key] = kv.Value with { SessionId = null };
                return;
            }
        }
    }

    /// <summary>
    /// Tries to find an entry by window key.
    /// </summary>
    public bool TryFindByKey(WindowKey key, out WindowRefEntry entry)
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            if (_entries.TryGetValue(key, out var found))
            {
                entry = found;
                return true;
            }
        }
        entry = default!;
        return false;
    }

    /// <summary>
    /// Lists all active (non-retired) window references.
    /// </summary>
    public IReadOnlyList<WindowRefEntry> ListActive()
    {
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            return _entries.Values.Where(e => !e.Retired).ToArray();
        }
    }

    /// <summary>
    /// Tries to find an entry by session ID.
    /// </summary>
    public bool TryFindBySessionId(string sessionId, out WindowRefEntry entry)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());
            foreach (var e in _entries.Values)
            {
                if (e.Retired) continue;
                if (string.Equals(e.SessionId, sessionId, StringComparison.Ordinal))
                {
                    entry = e;
                    return true;
                }
            }
        }
        entry = default!;
        return false;
    }

    /// <summary>
    /// Removes all entries associated with a session ID.
    /// </summary>
    public void RemoveBySessionId(string sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        lock (_lock)
        {
            PurgeExpiredRetiredEntriesCore(_utcNow());

            List<WindowKey>? toRemove = null;
            foreach (var kv in _entries)
            {
                if (!string.Equals(kv.Value.SessionId, sessionId, StringComparison.Ordinal)) continue;
                (toRemove ??= []).Add(kv.Key);
            }

            if (toRemove is null) return;
            foreach (var key in toRemove)
            {
                _entries.Remove(key);
            }
        }
    }

    internal int PurgeExpiredRetiredEntries()
    {
        lock (_lock)
        {
            return PurgeExpiredRetiredEntriesCore(_utcNow());
        }
    }

    private int PurgeExpiredRetiredEntriesCore(DateTimeOffset now)
    {
        List<WindowKey>? expired = null;
        foreach (var kv in _entries)
        {
            if (!kv.Value.Retired || kv.Value.RetiredAtUtc is null) continue;
            if (now - kv.Value.RetiredAtUtc.Value < _retiredEntryTtl) continue;
            (expired ??= []).Add(kv.Key);
        }

        if (expired is null) return 0;
        foreach (var key in expired)
        {
            _entries.Remove(key);
        }

        return expired.Count;
    }
}

/// <summary>
/// Represents a tracked window reference.
/// </summary>
public sealed record WindowRefEntry(
    string WindowRef,
    WindowKey Key,
    WindowInfo Info,
    string? SessionId,
    bool Retired,
    DateTimeOffset? RetiredAtUtc = null);
