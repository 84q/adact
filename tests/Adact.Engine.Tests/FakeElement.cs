using Adact.Engine.Elements;

namespace Adact.Engine.Tests;

/// <summary>L1/L2 テスト用の in-memory IElement 実装。</summary>
internal sealed class FakeElement : IElement
{
    public string? Name { get; set; }
    public string? AutomationId { get; set; }
    public string ControlType { get; set; } = "Unknown";
    public string? ClassName { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsOffscreen { get; set; }
    public string? Value { get; set; }
    public string? HelpText { get; set; }
    public Rect BoundingRectangle { get; set; }
    public bool IsKeyboardFocusable { get; set; }
    public bool HasKeyboardFocus { get; set; }
    public IReadOnlyList<int>? RuntimeId { get; init; }
    public List<IElement> ChildList { get; } = new();
    public IReadOnlyList<IElement> Children => ChildList;

    public int ClickCount { get; private set; }
    public string? LastFilledText { get; private set; }
    public int FocusCount { get; private set; }

    public void Click() => ClickCount++;
    public void Fill(string text) => LastFilledText = text;
    public void Focus() => FocusCount++;

    public FakeElement AddChild(FakeElement child)
    {
        ChildList.Add(child);
        return child;
    }

    public static FakeElement Window(string title, params FakeElement[] children)
    {
        var w = new FakeElement { ControlType = "Window", Name = title };
        foreach (var c in children) w.ChildList.Add(c);
        return w;
    }

    public static FakeElement Button(string name, string? automationId = null, string? helpText = null)
        => new() { ControlType = "Button", Name = name, AutomationId = automationId, HelpText = helpText };

    public static FakeElement Edit(string? value = null, string? automationId = null)
        => new() { ControlType = "Edit", AutomationId = automationId, Value = value };

    public static FakeElement Pane(string? name = null, params FakeElement[] children)
    {
        var p = new FakeElement { ControlType = "Pane", Name = name };
        foreach (var c in children) p.ChildList.Add(c);
        return p;
    }

    public static FakeElement Group(string? name = null, params FakeElement[] children)
    {
        var g = new FakeElement { ControlType = "Group", Name = name };
        foreach (var c in children) g.ChildList.Add(c);
        return g;
    }
}
