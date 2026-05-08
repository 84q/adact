using Adact.Engine.Elements;

namespace Adact.Engine;

/// <summary>
/// Describes an ancestor used when suggesting selectors.
/// </summary>
public sealed record AncestorInfo(string? AutomationId, string? Name, string ControlType);

/// <summary>
/// Suggests a selector path for an element.
/// </summary>
/// <param name="Stability">Selector confidence level.</param>
/// <param name="Code">The generated selector code.</param>
public sealed record SelectorSuggestion(string Stability, string Code);

/// <summary>
/// Suggests stable selectors for UIA elements.
/// </summary>
internal static class SelectorSuggester
{
    /// <summary>
    /// Suggests a selector for the given target.
    /// </summary>
    public static SelectorSuggestion? Suggest(
        IElement target,
        IEnumerable<IElement> allElements,
        IReadOnlyList<AncestorInfo> ancestors)
    {
        var all = allElements as IReadOnlyList<IElement> ?? allElements.ToList();

        if (!string.IsNullOrEmpty(target.AutomationId))
        {
            int count = CountByAutomationId(all, target.AutomationId);
            if (count == 1)
                return new SelectorSuggestion("High", $"cf.ByAutomationId(\"{target.AutomationId}\")");
        }

        if (!string.IsNullOrEmpty(target.Name))
        {
            int count = CountByNameAndControlType(all, target.Name, target.ControlType);
            if (count == 1)
                return new SelectorSuggestion("High", $"cf.ByName(\"{target.Name}\").And(cf.ByControlType(ControlType.{target.ControlType}))");
        }

        for (int i = 0; i < ancestors.Count; i++)
        {
            var ancestor = ancestors[i];

            string? ancestorPrefix = null;
            IReadOnlyList<IElement>? scopeElements = null;

            if (!string.IsNullOrEmpty(ancestor.AutomationId)
                && CountByAutomationId(all, ancestor.AutomationId) == 1)
            {
                ancestorPrefix = $"window.FindFirstDescendant(cf.ByAutomationId(\"{ancestor.AutomationId}\"))";
                scopeElements = GetScopeElementsByAutomationId(all, ancestor.AutomationId);
            }
            else if (!string.IsNullOrEmpty(ancestor.Name)
                && CountByNameAndControlType(all, ancestor.Name, ancestor.ControlType) == 1)
            {
                ancestorPrefix = $"window.FindFirstDescendant(cf.ByName(\"{ancestor.Name}\").And(cf.ByControlType(ControlType.{ancestor.ControlType})))";
                scopeElements = GetScopeElementsByNameAndControlType(all, ancestor.Name, ancestor.ControlType);
            }

            if (ancestorPrefix is null || scopeElements is null)
                continue;

            if (!string.IsNullOrEmpty(target.AutomationId))
            {
                int count = CountByAutomationId(scopeElements, target.AutomationId);
                if (count == 1)
                    return new SelectorSuggestion("High", $"{ancestorPrefix}.FindFirstDescendant(cf.ByAutomationId(\"{target.AutomationId}\"))");
            }

            if (!string.IsNullOrEmpty(target.Name))
            {
                int count = CountByNameAndControlType(scopeElements, target.Name, target.ControlType);
                if (count == 1)
                    return new SelectorSuggestion("Medium", $"{ancestorPrefix}.FindFirstDescendant(cf.ByName(\"{target.Name}\").And(cf.ByControlType(ControlType.{target.ControlType})))");
            }

            int index = IndexByControlType(scopeElements, target);
            if (index >= 0)
                return new SelectorSuggestion("Low", $"{ancestorPrefix}.FindAllDescendants(cf.ByControlType(ControlType.{target.ControlType}))[{index}]");
        }

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
    /// </summary>
    private static List<IElement> GetScopeElementsByAutomationId(IReadOnlyList<IElement> allElements, string ancestorAutomationId)
    {
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
