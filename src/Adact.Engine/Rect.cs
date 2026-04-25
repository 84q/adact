namespace Adact.Engine;

/// <summary>UIA BoundingRectangle を [x, y, w, h] で表現する単純な構造体。</summary>
public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    public int[] ToArray() => new[] { X, Y, Width, Height };
}
