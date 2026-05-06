using FlaUI.Core.WindowsAPI;

namespace Adact.Mcp.Common.InputDrivers;

/// <summary>
/// 低レベルキーボード操作の抽象化。テストでは Fake 実装に差し替え可能。
/// </summary>
public interface IKeyboardDriver
{
    /// <summary>キーを押して離す (1 回の Type)。</summary>
    void TypeKey(VirtualKeyShort key);

    /// <summary>キーを押し下げたままにする。</summary>
    void PressKey(VirtualKeyShort key);

    /// <summary>キーを解放する。</summary>
    void ReleaseKey(VirtualKeyShort key);
}
