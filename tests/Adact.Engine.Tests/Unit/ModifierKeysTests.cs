using FlaUI.Core.WindowsAPI;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// <see cref="ModifierKeys.Resolve"/> の修飾キー名解決を検証する Unit テスト。
/// Phase 8 設計 §6 (マウス / キーボード操作の修飾キー指定) の互換性回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class ModifierKeysTests
{
    /// <summary>null / 空配列は空リストを返す。</summary>
    [Fact]
    public void Resolve_Null_ReturnsEmpty()
    {
        Assert.Empty(ModifierKeys.Resolve(null));
        Assert.Empty(ModifierKeys.Resolve(System.Array.Empty<string>()));
    }

    /// <summary>"Shift" / "Control" / "Alt" / "Meta" / "Win" / "Windows" がそれぞれ対応する VK に解決される。</summary>
    [Theory]
    [InlineData("Shift", VirtualKeyShort.SHIFT)]
    [InlineData("Control", VirtualKeyShort.CONTROL)]
    [InlineData("Ctrl", VirtualKeyShort.CONTROL)]
    [InlineData("Alt", VirtualKeyShort.ALT)]
    [InlineData("Meta", VirtualKeyShort.LWIN)]
    [InlineData("Win", VirtualKeyShort.LWIN)]
    [InlineData("Windows", VirtualKeyShort.LWIN)]
    public void Resolve_KnownNames_MapToVk(string name, VirtualKeyShort expected)
    {
        var result = ModifierKeys.Resolve(new[] { name });
        Assert.Single(result, expected);
    }

    /// <summary>未知名は <see cref="System.ArgumentException"/>。</summary>
    [Fact]
    public void Resolve_Unknown_Throws()
    {
        Assert.Throws<System.ArgumentException>(() => ModifierKeys.Resolve(new[] { "Hyper" }));
    }
}
