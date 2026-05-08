namespace Adact.Engine;

/// <summary>
/// Options for <see cref="WindowSession.ClickAsync(string, ClickOptions?, System.Threading.CancellationToken)"/>.
/// Supports single-click, multi-click, modifier keys, and an optional position.
/// </summary>
/// <param name="Double">When true, uses the OS double-click gesture (added for the Phase 8 <c>doubleclick</c> API).</param>
/// <param name="Button">Mouse button to use. Defaults to <see cref="MouseButton.Left"/>.</param>
/// <param name="Count">Number of clicks to send (1 by default). Ignored for OS double-click.</param>
/// <param name="Modifiers">Modifier keys to hold (Shift / Control / Alt / Meta / Win / Windows). Null or empty means none.</param>
/// <param name="PositionX">X offset from the bounding rectangle's top-left corner in pixels. Null uses the center.</param>
/// <param name="PositionY">Y offset from the bounding rectangle's top-left corner in pixels. Null uses the center.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "Keep the original public API until ClickOptions gains real option fields.")]
public sealed record ClickOptions(
    bool Double = false,
    MouseButton Button = MouseButton.Left,
    int Count = 1,
    System.Collections.Generic.IReadOnlyList<string>? Modifiers = null,
    int? PositionX = null,
    int? PositionY = null);
