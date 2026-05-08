namespace Adact.Engine;

/// <summary>
/// Result returned by <see cref="WindowSession.InspectAsync(string, CancellationToken)"/>.
/// Includes the raw UIA fields used by design 022 §8 and a JSON-friendly shape for MCP / CLI output.
/// </summary>
/// <param name="Ref">Element Ref ID (for example, <c>s1e7</c>).</param>
/// <param name="Name">UIA Name. Can be <c>null</c>.</param>
/// <param name="ControlType">UIA ControlType (for example, <c>"Button"</c>).</param>
/// <param name="AutomationId">UIA AutomationId. Can be <c>null</c>.</param>
/// <param name="ClassName">Win32 class name. Can be <c>null</c>.</param>
/// <param name="HelpText">UIA HelpText. Can be <c>null</c>.</param>
/// <param name="Value">ValuePattern.Value. Null when the pattern is unavailable.</param>
/// <param name="BoundingRect">UIA bounding rectangle.</param>
/// <param name="IsEnabled">Whether the element is enabled.</param>
/// <param name="IsOffscreen">Whether the element is offscreen.</param>
/// <param name="IsKeyboardFocusable">Whether the element can receive keyboard focus.</param>
/// <param name="HasKeyboardFocus">Whether the element currently has keyboard focus.</param>
/// <param name="Patterns">Available UIA patterns and their data.</param>
/// <param name="Selector">Selector suggestion, if any.</param>
public sealed record InspectResult(
    string Ref,
    string? Name,
    string ControlType,
    string? AutomationId,
    string? ClassName,
    string? HelpText,
    string? Value,
    Rect BoundingRect,
    bool IsEnabled,
    bool IsOffscreen,
    bool IsKeyboardFocusable,
    bool HasKeyboardFocus,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> Patterns,
    SelectorSuggestion? Selector = null);
