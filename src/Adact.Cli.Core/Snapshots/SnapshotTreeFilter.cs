namespace Adact.Cli.Snapshots;

/// <summary>
/// CLI 側の snapshot ツリーフィルタ。Phase 7 で Engine から移譲された
/// operable / raw 切替ロジックを担う。設計 016 §2、§2.4。
///
/// - <c>raw</c>: ツリーをそのまま返す (フィールド削減のみ)。
/// - <c>operable</c>: AlwaysInclude な ControlType は残し、Pane/Group/Custom 等は Name か
///   AutomationId があれば残し、なければ flatten。未知 ControlType は flatten。
///   IsOffscreen=true は子孫ごと exclude する。
///
/// ルート要素は常に保持する (SnapshotBuilder 同様)。
/// </summary>
internal static class SnapshotTreeFilter
{
    /// <summary>operable フィルタ名 (逆引き出力・検査用に使用)。</summary>
    public const string FilterOperable = "operable";

    /// <summary>raw フィルタ名 (ツリーをそのまま保持)。</summary>
    public const string FilterRaw = "raw";

    /// <summary>operable フィルタで常に保持する ControlType 名集合。</summary>
    private static readonly HashSet<string> AlwaysInclude = new(StringComparer.OrdinalIgnoreCase)
  {
    "Window", "Button", "MenuItem", "Edit", "CheckBox", "RadioButton",
    "ComboBox", "Tab", "TabItem", "Tree", "TreeItem", "List", "ListItem",
    "Hyperlink", "Slider", "Spinner", "SplitButton", "Document", "Text",
    "Menu", "MenuBar", "ToolBar", "TitleBar", "StatusBar", "DataGrid", "DataItem",
    "Header", "HeaderItem", "Table",
  };

    /// <summary>name/AutomationId がなければ flatten される構造系 ControlType 名集合。</summary>
    private static readonly HashSet<string> StructuralFlattenCandidates = new(StringComparer.OrdinalIgnoreCase)
  {
    "Pane", "Group", "Custom", "Thumb", "Image", "Separator",
  };

    /// <summary>指定フィルタ名が CLI で扱える既知のものか確認する。</summary>
    /// <param name="filter">フィルタ名 (大文字小文字不問)。</param>
    /// <returns><c>operable</c> / <c>raw</c> のいずれかに一致すれば true。</returns>
    public static bool IsKnownFilter(string filter)
      => string.Equals(filter, FilterOperable, StringComparison.OrdinalIgnoreCase)
      || string.Equals(filter, FilterRaw, StringComparison.OrdinalIgnoreCase);

    /// <summary>正規化済みの filter 名 (小文字) を返す。</summary>
    /// <param name="filter">入力フィルタ名。</param>
    /// <returns>小文字化した filter 名。</returns>
    public static string Normalize(string filter)
      => filter.ToLowerInvariant();

    /// <summary>ルート要素を起点にフィルタを適用する。ルートは常に保持する。</summary>
    /// <param name="root">フィルタを適用するツリーのルート要素。</param>
    /// <param name="filter">適用するフィルタ名 (事前に <see cref="IsKnownFilter"/> で検査しておくこと)。</param>
    /// <returns>フィルタ適用後のツリー (ルートは入力と同一、もしくは children のみ差し替えされたコピー)。</returns>
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

    /// <summary>ルート要素の子リストについてフィルタを適用し、新しいリストを返す。</summary>
    /// <param name="children">適用対象の子要素リスト。</param>
    /// <returns>フィルタ適用後の子要素リスト (flatten / 除外を反映済み)。</returns>
    private static List<SnapshotElement> FilterChildren(IReadOnlyList<SnapshotElement> children)
    {
        var output = new List<SnapshotElement>();
        foreach (var c in children)
        {
            ApplyDecision(c, output);
        }
        return output;
    }

    /// <summary>1 要素について保持 / flatten / 除外 を判断し、<paramref name="parentOut"/> に追記する。</summary>
    /// <param name="el">判定対象の要素。</param>
    /// <param name="parentOut">保持もしくは flatten された子を書き出す親側リスト。</param>
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
