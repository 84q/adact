using FlaUI.Core.WindowsAPI;

namespace Adact.Mcp.Common.InputDrivers;

/// <summary>
/// Provides keyboard input operations.
/// </summary>
public interface IKeyboardDriver
{
    void TypeKey(VirtualKeyShort key);

    void PressKey(VirtualKeyShort key);

    void ReleaseKey(VirtualKeyShort key);
}
