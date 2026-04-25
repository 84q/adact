using Adact.Engine.Elements;

namespace Adact.Engine.Filters;

/// <summary>
/// 既定のフィルタ戦略。AI が操作可能な要素を中心に残し、
/// 無名 Pane / Group / Custom は flatten、IsOffscreen=true は exclude する。
/// プロパティは Phase 2 のホワイトリストに従って出力。
/// </summary>
public sealed class OperableFilterStrategy : IFilterStrategy
{
  public string Name => "operable";

  private static readonly HashSet<string> AlwaysInclude = new(StringComparer.OrdinalIgnoreCase)
    {
        "Window", "Button", "MenuItem", "Edit", "CheckBox", "RadioButton",
        "ComboBox", "Tab", "TabItem", "Tree", "TreeItem", "List", "ListItem",
        "Hyperlink", "Slider", "Spinner", "SplitButton", "Document", "Text",
        "Menu", "MenuBar", "ToolBar", "TitleBar", "StatusBar", "DataGrid", "DataItem",
        "Header", "HeaderItem", "Table",
    };

  private static readonly HashSet<string> StructuralFlattenCandidates = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pane", "Group", "Custom", "Thumb", "Image", "Separator",
    };

  public NodeDecision Decide(IElement el, FilterContext ctx)
  {
    // Root ウィンドウのガードは SnapshotBuilder 側で行うため、ここでは行わない。

    // 画面外要素は除外
    if (el.IsOffscreen) return NodeDecision.Exclude;

    var ct = el.ControlType;

    if (AlwaysInclude.Contains(ct))
    {
      // Pane/Group が誤って AlwaysInclude に来た場合の防御 (実害無いが念のため)
      return NodeDecision.Include;
    }

    if (StructuralFlattenCandidates.Contains(ct))
    {
      // Name または AutomationId があれば構造として残す。なければ flatten
      if (!string.IsNullOrEmpty(el.Name) || !string.IsNullOrEmpty(el.AutomationId))
        return NodeDecision.Include;
      return NodeDecision.Flatten;
    }

    // 未知の ControlType は安全側で flatten (子は残す)
    return NodeDecision.Flatten;
  }

  public IReadOnlyDictionary<string, object?> ExtractProperties(IElement el)
  {
    var dict = new Dictionary<string, object?>();
    if (!string.IsNullOrEmpty(el.Name)) dict["name"] = el.Name;
    if (!string.IsNullOrEmpty(el.AutomationId)) dict["automationId"] = el.AutomationId;
    if (!string.IsNullOrEmpty(el.ClassName)) dict["className"] = el.ClassName;
    if (!el.IsEnabled) dict["isEnabled"] = false; // 既定 true なので false の時のみ出す
    if (el.IsOffscreen) dict["isOffscreen"] = true;
    if (!string.IsNullOrEmpty(el.Value)) dict["value"] = el.Value;
    if (!string.IsNullOrEmpty(el.HelpText)) dict["helpText"] = el.HelpText;
    var r = el.BoundingRectangle;
    if (r.Width != 0 || r.Height != 0 || r.X != 0 || r.Y != 0)
      dict["boundingRect"] = new[] { r.X, r.Y, r.Width, r.Height };
    if (el.IsKeyboardFocusable) dict["isKeyboardFocusable"] = true;
    if (el.HasKeyboardFocus) dict["hasKeyboardFocus"] = true;
    return dict;
  }
}
