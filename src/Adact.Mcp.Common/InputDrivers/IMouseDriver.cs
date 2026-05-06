using Adact.Engine;

namespace Adact.Mcp.Common.InputDrivers;

/// <summary>
/// 低レベルマウス操作の抽象化。テストでは Fake 実装に差し替え可能。
/// </summary>
public interface IMouseDriver
{
    /// <summary>マウスカーソルを絶対座標 (x, y) に移動する。</summary>
    void MoveTo(int x, int y);

    /// <summary>現在カーソル位置で指定ボタンを押下する。</summary>
    void Down(MouseButton button);

    /// <summary>現在カーソル位置で指定ボタンを解放する。</summary>
    void Up(MouseButton button);

    /// <summary>垂直ホイールをスクロールする。正値=下方向。</summary>
    void Scroll(int amount);

    /// <summary>水平ホイールをスクロールする。正値=右方向。</summary>
    void HorizontalScroll(int amount);
}
