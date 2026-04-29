namespace Adact.Cli.Snapshots;

/// <summary>
/// CLI 側の snapshot ツリーフィルタ。Phase 7 で <see cref="Adact.Engine"/> から移譲された
/// operable / raw 切替ロジックを担う。設計 016 §2、§2.4。
///
/// - <c>raw</c>: ツリーをそのまま返す (フィールド削減のみ)。
/// - <c>operable</c>: AlwaysInclude な ControlType は残し、Pane/Group/Custom 等は Name か
///   AutomationId があれば残し、なければ flatten。未知 ControlType は flatten。
///   IsOffscreen=true は子孫ごと exclude する。
///
/// ルート要素は常に保持する (<see cref="Adact.Engine.Snapshot.SnapshotBuilder"/> 同様)。
/// </summary>
internal static class SnapshotTreeFilter
{
    public const string FilterOperable = "operable";
    public const string FilterRaw = "raw";

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

    /// <summary>指定フィルタ名が CLI で扱える既知のものか確認する。</summary>
    public static bool IsKnownFilter(string filter)
      => string.Equals(filter, FilterOperable, StringComparison.OrdinalIgnoreCase)
      || string.Equals(filter, FilterRaw, StringComparison.OrdinalIgnoreCase);

    /// <summary>正規化済みの filter 名 (小文字) を返す。</summary>
    public static string Normalize(string filter)
      => filter.ToLowerInvariant();

    /// <summary>ルート要素を起点にフィルタを適用する。ルートは常に保持する。</summary>
    public static SnapshotElement Apply(SnapshotElement root, string filter)
    {
        var normalized = Normalize(filter);
        if (normalized == FilterRaw)
        {
            // raw: ツリー構造はそのまま。フィールド削減はテキスト整形側で行う。
            return root;
        }

        // operable: ルートはガード (IsOffscreen であっても保持)。子のみフィルタを適用。
        var children = FilterChildren(root.Children);
        return root with { Children = children };
    }

    private static List<SnapshotElement> FilterChildren(IReadOnlyList<SnapshotElement> children)
    {
        var output = new List<SnapshotElement>();
        foreach (var c in children)
        {
            ApplyDecision(c, output);
        }
        return output;
    }

    private static void ApplyDecision(SnapshotElement el, List<SnapshotElement> parentOut)
    {
        // モーダルダイアログは Engine が isModalDialog=true で root の子として注入する。
        // フィルタ判定では通常のサブツリーと同じ規則で扱う (Window 扱いで AlwaysInclude)。
        if (el.IsOffscreen)
        {
            // 子孫ごと除外。
            return;
        }

        if (AlwaysInclude.Contains(el.Role))
        {
            var filteredChildren = FilterChildren(el.Children);
            parentOut.Add(el with { Children = filteredChildren });
            return;
        }

        if (StructuralFlattenCandidates.Contains(el.Role))
        {
            if (!string.IsNullOrEmpty(el.Name) || !string.IsNullOrEmpty(el.AutomationId))
            {
                var filteredChildren = FilterChildren(el.Children);
                parentOut.Add(el with { Children = filteredChildren });
                return;
            }
            // flatten: 自身は省き、子を親に昇格させる。
            foreach (var grand in el.Children)
            {
                ApplyDecision(grand, parentOut);
            }
            return;
        }

        // 未知 ControlType は安全側で flatten。
        foreach (var grand in el.Children)
        {
            ApplyDecision(grand, parentOut);
        }
    }
}
