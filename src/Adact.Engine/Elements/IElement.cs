namespace Adact.Engine.Elements;

/// <summary>
/// Represents a UI automation element exposed by the engine.
/// </summary>
public interface IElement
{
    /// <summary>
    /// Gets the UIA Name.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the UIA AutomationId.
    /// </summary>
    string? AutomationId { get; }

    /// <summary>
    /// Gets the UIA control type.
    /// </summary>
    string ControlType { get; }

    /// <summary>
    /// Gets the Win32 class name.
    /// </summary>
    string? ClassName { get; }

    /// <summary>
    /// Gets whether the element is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets whether the element is selected.
    /// </summary>
    bool IsSelected { get; }

    /// <summary>
    /// Gets whether the element is offscreen.
    /// </summary>
    bool IsOffscreen { get; }

    /// <summary>
    /// Gets the element value, if any.
    /// </summary>
    string? Value { get; }

    /// <summary>
    /// Gets the UIA help text, if any.
    /// </summary>
    string? HelpText { get; }

    /// <summary>
    /// Gets the bounding rectangle.
    /// </summary>
    Rect BoundingRectangle { get; }

    /// <summary>
    /// Gets whether the element can receive keyboard focus.
    /// </summary>
    bool IsKeyboardFocusable { get; }

    /// <summary>
    /// Gets whether the element currently has keyboard focus.
    /// </summary>
    bool HasKeyboardFocus { get; }

    /// <summary>
    /// Gets the UIA runtime ID.
    /// </summary>
    IReadOnlyList<int>? RuntimeId { get; }

    /// <summary>
    /// Gets the child elements.
    /// </summary>
    IReadOnlyList<IElement> Children { get; }

    /// <summary>
    /// Clears any cached child elements.
    /// </summary>
    void ClearChildrenCache();

    /// <summary>
    /// Clicks the element.
    /// </summary>
    void Click();

    /// <summary>
    /// Fills text into the element.
    /// </summary>
    void Fill(string text);

    /// <summary>
    /// Focuses the element.
    /// </summary>
    void Focus();
}

/// <summary>
/// Represents an element that can be checked or unchecked.
/// </summary>
public interface ICheckableElement
{
    /// <summary>
    /// Gets whether the element is checked.
    /// </summary>
    bool IsChecked { get; }

    /// <summary>
    /// Sets the checked state.
    /// </summary>
    void SetChecked(bool isChecked);
}

/// <summary>
/// Represents an element that can select child items.
/// </summary>
public interface ISelectableElement
{
    /// <summary>
    /// Selects items inside the element.
    /// </summary>
    void SelectItems(SelectionTarget[] targets, SelectionMode mode);
}

/// <summary>
/// Represents an element that can scroll into view.
/// </summary>
public interface IScrollableElement
{
    /// <summary>
    /// Scrolls the element into view.
    /// </summary>
    void ScrollIntoView();
}
