using System.Globalization;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;

namespace Adact.Engine.Snapshot;

/// <summary>
/// Session スコープの Ref ID レジストリ。
/// StableKey (RuntimeId or positional fallback) → eid のマップを session lifetime で保持し、
/// 同一要素は再 snapshot 後も同じ eid を返す (設計 011 §4.2)。
/// 古い refId / 別 Session の refId 解決時は <see cref="RefNotFoundException"/>。
/// </summary>
public sealed class RefRegistry
{
    private readonly int _sessionId;
    private readonly Dictionary<string, int> _stableKeyToEid = new(StringComparer.Ordinal);
    private readonly Dictionary<int, IElement> _byElementIdInCurrentSnapshot = new();
    private int _nextEid = 1;

    public RefRegistry(int sessionId)
    {
        _sessionId = sessionId;
    }

    public int SessionId => _sessionId;

    /// <summary>新しい snapshot 用に「現 snapshot 登録済みの eid 集合」をクリアする。</summary>
    public void BeginSnapshot()
    {
        _byElementIdInCurrentSnapshot.Clear();
    }

    /// <summary>
    /// 要素を登録し、対応する Ref ID を返す。StableKey が既存なら同じ eid を再利用、
    /// 新規なら単調増加カウンタで採番する。
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

    /// <summary>Ref ID を解決し、対応する <see cref="IElement"/> を返す。</summary>
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

    private static string ComputeStableKey(IElement el, int positionalIndex)
    {
        var rid = el.RuntimeId;
        if (rid is { Count: > 0 })
            return "rid:" + string.Join("-", rid);
        return "unstable:" + positionalIndex.ToString(CultureInfo.InvariantCulture);
    }
}
