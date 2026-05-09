using Adact.Engine;

namespace Adact.Mcp.Common.InputDrivers;

/// <summary>
/// Provides mouse input operations.
/// </summary>
public interface IMouseDriver
{
    /// <summary>
    /// Moves the cursor to absolute screen coordinates.
    /// </summary>
    /// <param name="x">The target X coordinate in pixels.</param>
    /// <param name="y">The target Y coordinate in pixels.</param>
    void MoveTo(int x, int y);

    /// <summary>
    /// Presses and holds a mouse button.
    /// </summary>
    /// <param name="button">The mouse button to press.</param>
    void Down(MouseButton button);

    /// <summary>
    /// Releases a mouse button.
    /// </summary>
    /// <param name="button">The mouse button to release.</param>
    void Up(MouseButton button);

    /// <summary>
    /// Scrolls vertically by the specified amount.
    /// </summary>
    /// <param name="amount">The vertical scroll amount in implementation-defined units.</param>
    void Scroll(int amount);

    /// <summary>
    /// Scrolls horizontally by the specified amount.
    /// </summary>
    /// <param name="amount">The horizontal scroll amount in implementation-defined units.</param>
    void HorizontalScroll(int amount);
}
