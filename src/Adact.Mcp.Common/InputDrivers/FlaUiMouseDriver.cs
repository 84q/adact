using Adact.Engine;

using FlaUIMouse = FlaUI.Core.Input.Mouse;
using FlaUiMouseButton = FlaUI.Core.Input.MouseButton;

namespace Adact.Mcp.Common.InputDrivers;

/// <summary>
/// Mouse driver that forwards input to FlaUI.
/// </summary>
internal sealed class FlaUiMouseDriver : IMouseDriver
{
    public void MoveTo(int x, int y) => FlaUIMouse.MoveTo(x, y);

    public void Down(MouseButton button) => FlaUIMouse.Down(ToFlaUi(button));

    public void Up(MouseButton button) => FlaUIMouse.Up(ToFlaUi(button));

    public void Scroll(int amount) => FlaUIMouse.Scroll(amount);

    public void HorizontalScroll(int amount) => FlaUIMouse.HorizontalScroll(amount);

    private static FlaUiMouseButton ToFlaUi(MouseButton button) => button switch
    {
        MouseButton.Right => FlaUiMouseButton.Right,
        MouseButton.Middle => FlaUiMouseButton.Middle,
        _ => FlaUiMouseButton.Left,
    };
}
