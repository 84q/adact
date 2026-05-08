using Adact.Mcp.Common.InputDrivers;

using FlaUI.Core.WindowsAPI;

namespace Adact.Mcp.Common.Tests.Unit;

internal sealed class FakeKeyboardDriver : IKeyboardDriver
{
    /// <summary>Gets the Calls value.</summary>
    public List<string> Calls { get; } = [];

    /// <summary>Performs the Type Key operation.</summary>
    public void TypeKey(VirtualKeyShort key) => Calls.Add($"type:{key}");

    /// <summary>Performs the Press Key operation.</summary>
    public void PressKey(VirtualKeyShort key) => Calls.Add($"press:{key}");

    /// <summary>Performs the Release Key operation.</summary>
    public void ReleaseKey(VirtualKeyShort key) => Calls.Add($"release:{key}");
}
