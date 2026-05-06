using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace Adact.Mcp.Common.InputDrivers;

/// <summary>
/// 本番用 <see cref="IKeyboardDriver"/> 実装。FlaUI の <see cref="Keyboard"/> を直接呼ぶ。
/// </summary>
internal sealed class FlaUiKeyboardDriver : IKeyboardDriver
{
    public void TypeKey(VirtualKeyShort key) => Keyboard.Type(key);

    public void PressKey(VirtualKeyShort key) => Keyboard.Press(key);

    public void ReleaseKey(VirtualKeyShort key) => Keyboard.Release(key);
}
