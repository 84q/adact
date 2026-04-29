namespace Adact.Engine;

/// <summary>UIA BoundingRectangle を [x, y, w, h] で表現する単純な構造体。</summary>
/// <param name="X">左上隅の X 座標 (スクリーン座標、ピクセル単位)。</param>
/// <param name="Y">左上隅の Y 座標 (スクリーン座標、ピクセル単位)。</param>
/// <param name="Width">幅 (ピクセル単位)。</param>
/// <param name="Height">高さ (ピクセル単位)。</param>
public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    /// <summary><c>[X, Y, Width, Height]</c> 順の 4 要素 <see cref="int"/> 配列を生成する (Snapshot JSON 出力用)。</summary>
    /// <returns>新しい配列インスタンス。</returns>
    public int[] ToArray() => new[] { X, Y, Width, Height };
}
