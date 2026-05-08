using System.Globalization;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;

namespace Adact.Engine.Snapshot;

/// <summary>
/// Tracks element refs within a single snapshot.
/// </summary>
public sealed class RefRegistry
{
    private readonly int _sessionId;
    private readonly Dictionary<string, int> _stableKeyToEid = new(StringComparer.Ordinal);
    private readonly Dictionary<int, IElement> _byElementIdInCurrentSnapshot = new();
    private int _nextEid = 1;

    /// <summary>
    /// Creates a new registry for the given session ID.
    /// </summary>
    public RefRegistry(int sessionId)
    {
        _sessionId = sessionId;
    }

    /// <summary>
    /// Gets the current session ID.
    /// </summary>
    public int SessionId => _sessionId;

    /// <summary>
    /// Clears the current snapshot element map.
    /// </summary>
    public void BeginSnapshot()
    {
        _byElementIdInCurrentSnapshot.Clear();
    }

    /// <summary>
    /// Registers an element and returns its ref ID.
    /// </summary>
    public string Register(IElement el, int positionalIndex)
    {
        ArgumentNullException.ThrowIfNull(el);

        var stableKey = ComputeStableKey(el, positionalIndex);
        if (!_stableKeyToEid.TryGetValue(stableKey, out var eid))
        {
            eid = _nextEid++;
            _stableKeyToEid[stableKey] = eid;
        }
        _byElementIdInCurrentSnapshot[eid] = el;
        return RefId.Format(_sessionId, eid);
    }

    /// <summary>
    /// Resolves a ref ID back to the matching element in the current snapshot.
    /// </summary>
    /// <exception cref="RefNotFoundException">Thrown when the ref is malformed, belongs to another session, or is no longer present.</exception>
    public IElement Resolve(string refId)
    {
        if (!RefId.TryParse(refId, out var s, out var e))
            throw new RefNotFoundException(refId, "malformed ref id");

        if (s != _sessionId)
            throw new RefNotFoundException(refId, $"session mismatch (expected s{_sessionId})");

        if (!_byElementIdInCurrentSnapshot.TryGetValue(e, out var el))
            throw new RefNotFoundException(refId, "element not found in current snapshot — re-snapshot required");

        return el;
    }

    /// <summary>
    /// Computes a stable key for an element within the snapshot.
    /// </summary>
    private static string ComputeStableKey(IElement el, int positionalIndex)
    {
        var rid = el.RuntimeId;
        if (rid is { Count: > 0 })
            return "rid:" + string.Join("-", rid);
        return "unstable:" + positionalIndex.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Enumerates the elements in the current snapshot.
    /// </summary>
    internal IEnumerable<(string Ref, IElement Element)> EnumerateCurrent()
    {
        foreach (var (eid, el) in _byElementIdInCurrentSnapshot)
        {
            yield return (RefId.Format(_sessionId, eid), el);
        }
    }
}
