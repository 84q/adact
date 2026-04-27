namespace Adact.Engine.Elements;

/// <summary>
/// UIA 要素の Engine 内抽象。FlaUI 直接依存を避けて L2 テストで FakeElement を使えるようにする。
/// </summary>
public interface IElement
{
    string? Name { get; }
    string? AutomationId { get; }
    string ControlType { get; }
    string? ClassName { get; }
    bool IsEnabled { get; }
    bool IsOffscreen { get; }
    string? Value { get; }
    string? HelpText { get; }
    Rect BoundingRectangle { get; }
    bool IsKeyboardFocusable { get; }
    bool HasKeyboardFocus { get; }
    IReadOnlyList<IElement> Children { get; }
    void Click();
    void Fill(string text);
}
