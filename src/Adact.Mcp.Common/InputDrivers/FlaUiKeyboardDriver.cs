using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace Adact.Mcp.Common.InputDrivers;

/// <summary>
/// Keyboard driver that forwards input to FlaUI.
/// </summary>
internal sealed class FlaUiKeyboardDriver : IKeyboardDriver
{
    public void TypeKey(VirtualKeyShort key) => Keyboard.Type(key);

    public void PressKey(VirtualKeyShort key) => Keyboard.Press(key);

    public void ReleaseKey(VirtualKeyShort key) => Keyboard.Release(key);
}
