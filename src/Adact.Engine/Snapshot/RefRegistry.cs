using Adact.Engine.Elements;
using Adact.Engine.Exceptions;

namespace Adact.Engine.Snapshot;

/// <summary>
/// Session スコープの Ref ID レジストリ。
/// SnapshotAsync ごとに <see cref="StartNewGeneration"/> を呼び旧マッピングを破棄する。
/// 古い refId / 別 Session の refId 解決時は <see cref="RefNotFoundException"/>。
/// </summary>
public sealed class RefRegistry
{
  private readonly int _sessionId;
  private int _generation;
  private int _nextElementId;
  private Dictionary<int, IElement> _byElementId = new();

  public RefRegistry(int sessionId)
  {
    _sessionId = sessionId;
    _generation = 0;
  }

  public int SessionId => _sessionId;
  public int Generation => _generation;

  /// <summary>新しい snapshot 用に generation をインクリメントし、旧マッピングをクリアする。</summary>
  public void StartNewGeneration()
  {
    _generation++;
    _nextElementId = 1;
    _byElementId = new Dictionary<int, IElement>();
  }

  /// <summary>新しい elementId を採番し、要素を登録する。Ref ID を返す。</summary>
  public string Register(IElement el)
  {
    if (_generation == 0)
      throw new InvalidOperationException("StartNewGeneration must be called before Register.");
    var id = _nextElementId++;
    _byElementId[id] = el;
    return RefId.Format(_sessionId, _generation, id);
  }

  /// <summary>Ref ID を解決し、対応する <see cref="IElement"/> を返す。</summary>
  public IElement Resolve(string refId)
  {
    if (!RefId.TryParse(refId, out var s, out var g, out var e))
      throw new RefNotFoundException(refId, "malformed ref id");

    if (s != _sessionId)
      throw new RefNotFoundException(refId, $"session mismatch (expected s{_sessionId})");

    if (g != _generation)
      throw new RefNotFoundException(refId, $"generation mismatch (current g{_generation}, given g{g} — refresh snapshot)");

    if (!_byElementId.TryGetValue(e, out var el))
      throw new RefNotFoundException(refId, "element id not registered in current snapshot");

    return el;
  }
}
