using Adact.Engine;
using Adact.Engine.Elements;

namespace Adact.Engine.Tests;

internal sealed class FakeElement : IElement, ICheckableElement, ISelectableElement, IScrollableElement
{
    /// <summary>Gets or sets the Name value.</summary>
    public string? Name { get; set; }
    /// <summary>Gets or sets the Automation Id value.</summary>
    public string? AutomationId { get; set; }
    /// <summary>Gets or sets the Control Type value.</summary>
    public string ControlType { get; set; } = "Unknown";
    /// <summary>Gets or sets the Class Name value.</summary>
    public string? ClassName { get; set; }
    /// <summary>Gets or sets the Is Enabled value.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets the Is Selected value.</summary>
    public bool IsSelected { get; set; }
    /// <summary>Gets or sets the Is Offscreen value.</summary>
    public bool IsOffscreen { get; set; }
    /// <summary>Gets or sets the Value value.</summary>
    public string? Value { get; set; }
    /// <summary>Gets or sets the Help Text value.</summary>
    public string? HelpText { get; set; }
    /// <summary>Gets or sets the Bounding Rectangle value.</summary>
    public Rect BoundingRectangle { get; set; }
    /// <summary>Gets or sets the Is Keyboard Focusable value.</summary>
    public bool IsKeyboardFocusable { get; set; }
    /// <summary>Gets or sets the Has Keyboard Focus value.</summary>
    public bool HasKeyboardFocus { get; set; }
    /// <summary>Gets the Runtime Id value.</summary>
    public IReadOnlyList<int>? RuntimeId { get; init; }
    /// <summary>Performs the new operation.</summary>
    public List<IElement> ChildList { get; } = new();
    /// <summary>Gets the Children value.</summary>
    public IReadOnlyList<IElement> Children => ChildList;

    /// <summary>Performs the Clear Children Cache operation.</summary>
    public void ClearChildrenCache()
    {
    }

    /// <summary>Gets or sets the Click Count value.</summary>
    public int ClickCount { get; private set; }
    /// <summary>Gets or sets the Last Filled Text value.</summary>
    public string? LastFilledText { get; private set; }
    /// <summary>Gets or sets the Focus Count value.</summary>
    public int FocusCount { get; private set; }
    /// <summary>Gets or sets the Is Checked value.</summary>
    public bool IsChecked { get; private set; }
    /// <summary>Gets or sets the Last Set Checked value.</summary>
    public bool? LastSetChecked { get; private set; }
    /// <summary>Gets or sets the Last Selected Name value.</summary>
    public string? LastSelectedName { get; private set; }
    /// <summary>Gets or sets the Last Selected Index value.</summary>
    public int? LastSelectedIndex { get; private set; }
    /// <summary>Gets or sets the Last Selected Item value.</summary>
    public IElement? LastSelectedItem { get; private set; }
    /// <summary>Gets or sets the Last Selected Targets value.</summary>
    public SelectionTarget[]? LastSelectedTargets { get; private set; }
    /// <summary>Gets or sets the Last Selection Mode value.</summary>
    public SelectionMode LastSelectionMode { get; private set; }
    /// <summary>Gets or sets the Scroll Into View Count value.</summary>
    public int ScrollIntoViewCount { get; private set; }

    /// <summary>Performs the Click operation.</summary>
    public void Click() => ClickCount++;
    /// <summary>Performs the Fill operation.</summary>
    public void Fill(string text) => LastFilledText = text;
    /// <summary>Performs the Focus operation.</summary>
    public void Focus() => FocusCount++;
    /// <summary>Performs the Set Checked operation.</summary>
    public void SetChecked(bool isChecked)
    {
        IsChecked = isChecked;
        LastSetChecked = isChecked;
    }

    /// <summary>Performs the Select Items operation.</summary>
    public void SelectItems(SelectionTarget[] targets, SelectionMode mode)
    {
        LastSelectedTargets = targets;
        LastSelectionMode = mode;
        if (targets.Length > 0)
        {
            switch (targets[0])
            {
                case SelectionTarget.ByName byName:
                    LastSelectedName = byName.Name;
                    LastSelectedIndex = null;
                    LastSelectedItem = null;
                    break;
                case SelectionTarget.ByIndex byIndex:
                    LastSelectedName = null;
                    LastSelectedIndex = byIndex.Index;
                    LastSelectedItem = null;
                    break;
            }
        }
    }

    /// <summary>Performs the Scroll Into View operation.</summary>
    public void ScrollIntoView() => ScrollIntoViewCount++;

    /// <summary>Performs the Add Child operation.</summary>
    public FakeElement AddChild(FakeElement child)
    {
        ChildList.Add(child);
        return child;
    }

    /// <summary>Performs the Window operation.</summary>
    public static FakeElement Window(string title, params FakeElement[] children)
    {
        var w = new FakeElement { ControlType = "Window", Name = title };
        foreach (var c in children) w.ChildList.Add(c);
        return w;
    }

    /// <summary>Performs the Button operation.</summary>
    public static FakeElement Button(string name, string? automationId = null, string? helpText = null)
        => new() { ControlType = "Button", Name = name, AutomationId = automationId, HelpText = helpText };

    /// <summary>Performs the Edit operation.</summary>
    public static FakeElement Edit(string? value = null, string? automationId = null)
        => new() { ControlType = "Edit", AutomationId = automationId, Value = value };

    /// <summary>Performs the Pane operation.</summary>
    public static FakeElement Pane(string? name = null, params FakeElement[] children)
    {
        var p = new FakeElement { ControlType = "Pane", Name = name };
        foreach (var c in children) p.ChildList.Add(c);
        return p;
    }

    /// <summary>Performs the Group operation.</summary>
    public static FakeElement Group(string? name = null, params FakeElement[] children)
    {
        var g = new FakeElement { ControlType = "Group", Name = name };
        foreach (var c in children) g.ChildList.Add(c);
        return g;
    }
}
