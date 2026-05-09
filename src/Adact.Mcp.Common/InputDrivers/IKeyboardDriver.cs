using FlaUI.Core.WindowsAPI;

namespace Adact.Mcp.Common.InputDrivers;

/// <summary>
/// Provides keyboard input operations.
/// </summary>
public interface IKeyboardDriver
{
    /// <summary>
    /// Types a key by pressing and releasing it.
    /// </summary>
    /// <param name="key">The virtual key to type.</param>
    void TypeKey(VirtualKeyShort key);

    /// <summary>
    /// Presses and holds a key.
    /// </summary>
    /// <param name="key">The virtual key to press.</param>
    void PressKey(VirtualKeyShort key);

    /// <summary>
    /// Releases a previously pressed key.
    /// </summary>
    /// <param name="key">The virtual key to release.</param>
    void ReleaseKey(VirtualKeyShort key);
}
