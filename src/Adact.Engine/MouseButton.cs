namespace Adact.Engine;

/// <summary>
/// Mouse button values used by the engine.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "Standard mouse button names; aligns with FlaUI / Win32 / Playwright vocabulary.")]
public enum MouseButton
{
    /// <summary>
    /// The left mouse button.
    /// </summary>
    Left = 0,

    /// <summary>
    /// The right mouse button.
    /// </summary>
    Right = 1,

    /// <summary>
    /// The middle mouse button.
    /// </summary>
    Middle = 2,
}
