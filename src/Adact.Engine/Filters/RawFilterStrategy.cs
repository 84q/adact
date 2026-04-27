using Adact.Engine.Elements;

namespace Adact.Engine.Filters;

/// <summary>デバッグ用フィルタ。すべてのノードを Include し、フィルタを行わない。</summary>
public sealed class RawFilterStrategy : IFilterStrategy
{
    public string Name => "raw";

    public NodeDecision Decide(IElement el, FilterContext ctx) => NodeDecision.Include;

    public IReadOnlyDictionary<string, object?> ExtractProperties(IElement el)
    {
        var dict = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(el.Name)) dict["name"] = el.Name;
        if (!string.IsNullOrEmpty(el.AutomationId)) dict["automationId"] = el.AutomationId;
        if (!string.IsNullOrEmpty(el.ClassName)) dict["className"] = el.ClassName;
        dict["isEnabled"] = el.IsEnabled;
        dict["isOffscreen"] = el.IsOffscreen;
        if (!string.IsNullOrEmpty(el.Value)) dict["value"] = el.Value;
        if (!string.IsNullOrEmpty(el.HelpText)) dict["helpText"] = el.HelpText;
        var r = el.BoundingRectangle;
        dict["boundingRect"] = new[] { r.X, r.Y, r.Width, r.Height };
        dict["isKeyboardFocusable"] = el.IsKeyboardFocusable;
        dict["hasKeyboardFocus"] = el.HasKeyboardFocus;
        return dict;
    }
}
