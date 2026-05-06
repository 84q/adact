using Adact.Engine;
using Adact.Mcp.Common.InputDrivers;

namespace Adact.Mcp.Common.Tests.Unit;

internal sealed class FakeMouseDriver : IMouseDriver
{
    public List<string> Calls { get; } = [];

    public void MoveTo(int x, int y) => Calls.Add($"move:{x},{y}");

    public void Down(MouseButton button) => Calls.Add($"down:{button}");

    public void Up(MouseButton button) => Calls.Add($"up:{button}");

    public void Scroll(int amount) => Calls.Add($"scroll:{amount}");

    public void HorizontalScroll(int amount) => Calls.Add($"hscroll:{amount}");
}
