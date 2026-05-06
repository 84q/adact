using Adact.Mcp.Common.InputDrivers;

using FlaUI.Core.WindowsAPI;

namespace Adact.Mcp.Common.Tests.Unit;

internal sealed class FakeKeyboardDriver : IKeyboardDriver
{
    public List<string> Calls { get; } = [];

    public void TypeKey(VirtualKeyShort key) => Calls.Add($"type:{key}");

    public void PressKey(VirtualKeyShort key) => Calls.Add($"press:{key}");

    public void ReleaseKey(VirtualKeyShort key) => Calls.Add($"release:{key}");
}
