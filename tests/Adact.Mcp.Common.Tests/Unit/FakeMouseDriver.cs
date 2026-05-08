using Adact.Engine;
using Adact.Mcp.Common.InputDrivers;

namespace Adact.Mcp.Common.Tests.Unit;

internal sealed class FakeMouseDriver : IMouseDriver
{
    /// <summary>Gets the Calls value.</summary>
    public List<string> Calls { get; } = [];

    /// <summary>Performs the Move To operation.</summary>
    public void MoveTo(int x, int y) => Calls.Add($"move:{x},{y}");

    /// <summary>Performs the Down operation.</summary>
    public void Down(MouseButton button) => Calls.Add($"down:{button}");

    /// <summary>Performs the Up operation.</summary>
    public void Up(MouseButton button) => Calls.Add($"up:{button}");

    /// <summary>Performs the Scroll operation.</summary>
    public void Scroll(int amount) => Calls.Add($"scroll:{amount}");

    /// <summary>Performs the Horizontal Scroll operation.</summary>
    public void HorizontalScroll(int amount) => Calls.Add($"hscroll:{amount}");
}
