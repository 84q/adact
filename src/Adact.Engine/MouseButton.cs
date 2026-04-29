namespace Adact.Engine;

/// <summary>
/// クリック / マウス操作で指定可能なボタン種別。Phase 8 の <c>--button</c> 引数値に対応する。
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "Standard mouse button names; aligns with FlaUI / Win32 / Playwright vocabulary.")]
public enum MouseButton
{
    /// <summary>左ボタン (既定)。</summary>
    Left = 0,
    /// <summary>右ボタン。</summary>
    Right = 1,
    /// <summary>中央ボタン (ホイール押下)。</summary>
    Middle = 2,
}
