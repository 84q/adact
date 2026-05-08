namespace Adact.Engine;

/// <summary>
/// Represents a rectangle in screen coordinates.
/// </summary>
public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Returns the rectangle as <c>[x, y, width, height]</c>.
    /// </summary>
    public int[] ToArray() => new[] { X, Y, Width, Height };
}
