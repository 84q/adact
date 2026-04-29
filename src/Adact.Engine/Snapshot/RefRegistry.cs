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
    /// <summary>このレジストリが属するセッション ID。Ref ID の <c>s</c> 部に使われる。</summary>
    private readonly int _sessionId;
    /// <summary>
    /// StableKey (RuntimeId or positional fallback) → eid のマップ。Session lifetime で保持され、
    /// 同一要素には再 snapshot 後も同じ eid を返すために使う。
    /// </summary>
    private readonly Dictionary<string, int> _stableKeyToEid = new(StringComparer.Ordinal);
    /// <summary>現スナップショットで登録された eid の範囲 (eid → 要素)。<see cref="BeginSnapshot"/> でリセットされる。</summary>
    private readonly Dictionary<int, IElement> _byElementIdInCurrentSnapshot = new();
    /// <summary>次に払い出す eid。同一 StableKey に対しては既存値が再利用されるため、実体は「新規採番時のみインクリメントするカウンタ」。</summary>
    private int _nextEid = 1;

    /// <summary>新しいレジストリを指定セッション ID で初期化する。</summary>
    /// <param name="sessionId">レジストリが属するセッションの ID。</param>
    public RefRegistry(int sessionId)
    {
        _sessionId = sessionId;
    }

    /// <summary>このレジストリが属するセッション ID。</summary>
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
    /// <param name="el">登録する UIA 要素ラッパ。</param>
    /// <param name="positionalIndex">RuntimeId 取得不可時の StableKey フォールバックに用いる DFS 出現順インデックス。</param>
    /// <returns>登録結果に対応する Ref ID 文字列。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="el"/> が <c>null</c> の場合。</exception>
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
    /// <param name="refId">解決対象の Ref ID。</param>
    /// <returns>該当する UIA 要素ラッパ。</returns>
    /// <exception cref="RefNotFoundException">
    /// 形式不正、別セッションの Ref ID、または現 snapshot に未登録 (再 snapshot が必要) の場合。
    /// </exception>
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
    /// 要素に対応する StableKey を計算する。RuntimeId が取得可能なら <c>rid:</c> プレフィックス付きの連結文字列、
    /// 取得不可なら <c>unstable:</c> プレフィックス + DFS 出現順 index をフォールバックとして使う。
    /// </summary>
    /// <param name="el">対象要素。</param>
    /// <param name="positionalIndex">RuntimeId 不可時の DFS 出現順インデックス。</param>
    /// <returns>計算された StableKey 文字列。</returns>
    private static string ComputeStableKey(IElement el, int positionalIndex)
    {
        var rid = el.RuntimeId;
        if (rid is { Count: > 0 })
            return "rid:" + string.Join("-", rid);
        return "unstable:" + positionalIndex.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 現 snapshot で登録された <c>(refId, IElement)</c> 列を列挙する。<see cref="WindowSession"/> の wait-for 検索条件モード
    /// で snapshot 後に一致要素を探すために使う。snapshot を跨いだ呼び出しは想定しない。
    /// </summary>
    /// <returns>現スナップショット中の <c>(ref, element)</c> ペア列。</returns>
    internal IEnumerable<(string Ref, IElement Element)> EnumerateCurrent()
    {
        foreach (var (eid, el) in _byElementIdInCurrentSnapshot)
        {
            yield return (RefId.Format(_sessionId, eid), el);
        }
    }
}
