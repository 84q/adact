using Adact.Engine.Elements;

namespace Adact.Engine;

/// <summary>
/// inspect 対象要素の祖先チェーン 1 ノード分の情報。
/// </summary>
/// <param name="AutomationId">UIA AutomationId (null/空 = なし)。</param>
/// <param name="Name">UIA Name (null/空 = なし)。</param>
/// <param name="ControlType">UIA ControlType の文字列表現。</param>
public sealed record AncestorInfo(string? AutomationId, string? Name, string ControlType);

/// <summary>
/// 安定セレクタ候補の算出結果。
/// </summary>
/// <param name="Stability">"High" | "Medium" | "Low"。</param>
/// <param name="Code">FlaUI コード例 (1 行チェーン)。</param>
public sealed record SelectorSuggestion(string Stability, string Code);

/// <summary>
/// inspect 対象要素に対して安定セレクタ候補を算出する pure logic。
/// </summary>
internal static class SelectorSuggester
{
    /// <summary>
    /// 対象要素とウィンドウ内全要素・祖先チェーンから、最も安定性の高いセレクタ候補を 1 つ返す。
    /// </summary>
    /// <param name="target">対象要素。</param>
    /// <param name="allElements">ウィンドウ内全要素 (RefRegistry.EnumerateCurrent 由来)。</param>
    /// <param name="ancestors">対象要素の直接の親からルート方向に並ぶ祖先チェーン。</param>
    /// <returns>セレクタ候補。算出不能時は null。</returns>
    public static SelectorSuggestion? Suggest(
        IElement target,
        IEnumerable<IElement> allElements,
        IReadOnlyList<AncestorInfo> ancestors)
    {
        var all = allElements as IReadOnlyList<IElement> ?? allElements.ToList();

        // 1. AutomationId がウィンドウ全体でユニーク
        if (!string.IsNullOrEmpty(target.AutomationId))
        {
            int count = CountByAutomationId(all, target.AutomationId);
            if (count == 1)
                return new SelectorSuggestion("High", $"cf.ByAutomationId(\"{target.AutomationId}\")");
        }

        // 2. ControlType + Name がウィンドウ全体でユニーク
        if (!string.IsNullOrEmpty(target.Name))
        {
            int count = CountByNameAndControlType(all, target.Name, target.ControlType);
            if (count == 1)
                return new SelectorSuggestion("High", $"cf.ByName(\"{target.Name}\").And(cf.ByControlType(ControlType.{target.ControlType}))");
        }

        // 3. 祖先がウィンドウ全体からユニークに特定できる → 祖先スコープ内で絞り込み
        for (int i = 0; i < ancestors.Count; i++)
        {
            var ancestor = ancestors[i];

            string? ancestorPrefix = null;
            IReadOnlyList<IElement>? scopeElements = null;

            // 祖先を AutomationId で特定可能か
            if (!string.IsNullOrEmpty(ancestor.AutomationId)
                && CountByAutomationId(all, ancestor.AutomationId) == 1)
            {
                ancestorPrefix = $"window.FindFirstDescendant(cf.ByAutomationId(\"{ancestor.AutomationId}\"))";
                scopeElements = GetScopeElementsByAutomationId(all, ancestor.AutomationId);
            }
            // 祖先を Name + ControlType で特定可能か
            else if (!string.IsNullOrEmpty(ancestor.Name)
                && CountByNameAndControlType(all, ancestor.Name, ancestor.ControlType) == 1)
            {
                ancestorPrefix = $"window.FindFirstDescendant(cf.ByName(\"{ancestor.Name}\").And(cf.ByControlType(ControlType.{ancestor.ControlType})))";
                scopeElements = GetScopeElementsByNameAndControlType(all, ancestor.Name, ancestor.ControlType);
            }

            if (ancestorPrefix is null || scopeElements is null)
                continue;

            // 3a. 祖先スコープ内で自身の AutomationId がユニーク
            if (!string.IsNullOrEmpty(target.AutomationId))
            {
                int count = CountByAutomationId(scopeElements, target.AutomationId);
                if (count == 1)
                    return new SelectorSuggestion("High", $"{ancestorPrefix}.FindFirstDescendant(cf.ByAutomationId(\"{target.AutomationId}\"))");
            }

            // 3b. 祖先スコープ内で Name + ControlType がユニーク
            if (!string.IsNullOrEmpty(target.Name))
            {
                int count = CountByNameAndControlType(scopeElements, target.Name, target.ControlType);
                if (count == 1)
                    return new SelectorSuggestion("Medium", $"{ancestorPrefix}.FindFirstDescendant(cf.ByName(\"{target.Name}\").And(cf.ByControlType(ControlType.{target.ControlType})))");
            }

            // 3c. 祖先スコープ内で ControlType + Index
            int index = IndexByControlType(scopeElements, target);
            if (index >= 0)
                return new SelectorSuggestion("Low", $"{ancestorPrefix}.FindAllDescendants(cf.ByControlType(ControlType.{target.ControlType}))[{index}]");
        }

        // 4. 祖先に AutomationId なし → ウィンドウ全体で ControlType + Index
        int globalIndex = IndexByControlType(all, target);
        if (globalIndex >= 0)
            return new SelectorSuggestion("Low", $"window.FindAllDescendants(cf.ByControlType(ControlType.{target.ControlType}))[{globalIndex}]");

        return null;
    }

    private static int CountByAutomationId(IReadOnlyList<IElement> elements, string automationId)
    {
        int count = 0;
        for (int i = 0; i < elements.Count; i++)
        {
            if (string.Equals(elements[i].AutomationId, automationId, StringComparison.Ordinal))
                count++;
        }
        return count;
    }

    private static int CountByNameAndControlType(IReadOnlyList<IElement> elements, string name, string controlType)
    {
        int count = 0;
        for (int i = 0; i < elements.Count; i++)
        {
            var el = elements[i];
            if (string.Equals(el.Name, name, StringComparison.Ordinal)
                && string.Equals(el.ControlType, controlType, StringComparison.Ordinal))
                count++;
        }
        return count;
    }

    /// <summary>
    /// 祖先スコープ内の要素を取得する。allElements 中で祖先の AutomationId を持つ要素の子孫ツリーを再帰展開。
    /// </summary>
    private static List<IElement> GetScopeElementsByAutomationId(IReadOnlyList<IElement> allElements, string ancestorAutomationId)
    {
        // 祖先要素を見つける
        IElement? ancestorElement = null;
        for (int i = 0; i < allElements.Count; i++)
        {
            if (string.Equals(allElements[i].AutomationId, ancestorAutomationId, StringComparison.Ordinal))
            {
                ancestorElement = allElements[i];
                break;
            }
        }

        if (ancestorElement is null)
            return [];

        // 子孫ツリーを再帰展開してフラットリストにする
        var result = new List<IElement>();
        CollectDescendants(ancestorElement, result);
        return result;
    }

    private static List<IElement> GetScopeElementsByNameAndControlType(IReadOnlyList<IElement> allElements, string name, string controlType)
    {
        IElement? ancestorElement = null;
        for (int i = 0; i < allElements.Count; i++)
        {
            var el = allElements[i];
            if (string.Equals(el.Name, name, StringComparison.Ordinal)
                && string.Equals(el.ControlType, controlType, StringComparison.Ordinal))
            {
                ancestorElement = el;
                break;
            }
        }

        if (ancestorElement is null)
            return [];

        var result = new List<IElement>();
        CollectDescendants(ancestorElement, result);
        return result;
    }

    private static void CollectDescendants(IElement element, List<IElement> result)
    {
        foreach (var child in element.Children)
        {
            result.Add(child);
            CollectDescendants(child, result);
        }
    }

    /// <summary>
    /// elements 中で target と同じ ControlType を持つ要素の中での target の index を返す。
    /// target が見つからない場合は -1。
    /// </summary>
    private static int IndexByControlType(IReadOnlyList<IElement> elements, IElement target)
    {
        int index = 0;
        for (int i = 0; i < elements.Count; i++)
        {
            var el = elements[i];
            if (ReferenceEquals(el, target))
                return index;
            if (string.Equals(el.ControlType, target.ControlType, StringComparison.Ordinal))
                index++;
        }
        return -1;
    }
}
