using Adact.Engine.Elements;

namespace Adact.Engine.Filters;

/// <summary>
/// Snapshot ツリー構築時のフィルタ戦略。Decide でノードの扱いを決め、
/// ExtractProperties で JSON に出力するプロパティを返す。
/// </summary>
public interface IFilterStrategy
{
    string Name { get; }
    NodeDecision Decide(IElement el, FilterContext ctx);
    IReadOnlyDictionary<string, object?> ExtractProperties(IElement el);
}
