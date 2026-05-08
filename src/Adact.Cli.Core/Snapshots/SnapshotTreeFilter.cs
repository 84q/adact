namespace Adact.Cli.Snapshots;

/// <summary>
///
///
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

    public static bool IsKnownFilter(string filter)
      => string.Equals(filter, FilterOperable, StringComparison.OrdinalIgnoreCase)
      || string.Equals(filter, FilterRaw, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string filter)
      => filter.ToLowerInvariant();

    public static SnapshotElement Apply(SnapshotElement root, string filter)
    {
        var normalized = Normalize(filter);
        if (normalized == FilterRaw)
        {
            return root;
        }

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
        if (el.IsOffscreen)
        {
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
            foreach (var grand in el.Children)
            {
                ApplyDecision(grand, parentOut);
            }
            return;
        }

        foreach (var grand in el.Children)
        {
            ApplyDecision(grand, parentOut);
        }
    }
}
