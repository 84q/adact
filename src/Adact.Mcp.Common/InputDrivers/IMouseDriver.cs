using Adact.Engine;

namespace Adact.Mcp.Common.InputDrivers;

/// <summary>
/// Provides mouse input operations.
/// </summary>
public interface IMouseDriver
{
    void MoveTo(int x, int y);

    void Down(MouseButton button);

    void Up(MouseButton button);

    void Scroll(int amount);

    void HorizontalScroll(int amount);
}
