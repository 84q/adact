namespace Adact.Engine.Filters;

/// <summary>Snapshot ツリー構築時の各ノードの取り扱い。</summary>
public enum NodeDecision
{
  /// <summary>このノードを Snapshot に含める (子もそのまま処理)。</summary>
  Include,
  /// <summary>このノード自身は省くが、子は親に昇格させる (構造の連続性を保つ)。</summary>
  Flatten,
  /// <summary>このノードと子孫すべてを除外する。</summary>
  Exclude,
}
