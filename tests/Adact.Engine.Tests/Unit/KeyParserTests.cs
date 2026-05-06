using FlaUI.Core.WindowsAPI;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// <see cref="KeyParser"/> のキーコンボ解析を検証する Unit テスト。
/// Phase 8 設計 §6 (キーボード操作) の入力検証回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class KeyParserTests
{
    /// <summary>単一の英字キーは VK_KEY_X に解析される。</summary>
    [Fact]
    public void Parse_LetterOnly_ReturnsKeyEnum()
    {
        var (mods, main) = KeyParser.Parse("A");
        Assert.Empty(mods);
        Assert.Equal(VirtualKeyShort.KEY_A, main);
    }

    /// <summary>"Ctrl+S" は Control 修飾と KEY_S に解析される。</summary>
    [Fact]
    public void Parse_CtrlPlusLetter_ReturnsModifierAndMain()
    {
        var (mods, main) = KeyParser.Parse("Ctrl+S");
        Assert.Single(mods, VirtualKeyShort.CONTROL);
        Assert.Equal(VirtualKeyShort.KEY_S, main);
    }

    /// <summary>"F5" などの機能キーが解析できる。</summary>
    [Fact]
    public void Parse_FunctionKey_Resolves()
    {
        var (_, main) = KeyParser.Parse("F5");
        Assert.Equal(VirtualKeyShort.F5, main);
    }

    /// <summary>未知のキー名は <see cref="System.ArgumentException"/> を投げる。</summary>
    [Fact]
    public void Parse_UnknownKey_Throws()
    {
        Assert.Throws<System.ArgumentException>(() => KeyParser.Parse("Foo"));
    }

    /// <summary><see cref="KeyParser.ParseSingle"/> は修飾子付きを拒否する。</summary>
    [Fact]
    public void ParseSingle_RejectsCombo()
    {
        Assert.Throws<System.ArgumentException>(() => KeyParser.ParseSingle("Ctrl+A"));
    }

    /// <summary>"Meta+E" は LWin 修飾と KEY_E に解析される (Meta 別名)。</summary>
    [Fact]
    public void Parse_MetaModifier_ResolvesToLWin()
    {
        var (mods, main) = KeyParser.Parse("Meta+E");
        Assert.Single(mods, VirtualKeyShort.LWIN);
        Assert.Equal(VirtualKeyShort.KEY_E, main);
    }

    /// <summary>"Win+E" は LWin 修飾と KEY_E に解析される (Win 別名)。</summary>
    [Fact]
    public void Parse_WinModifier_ResolvesToLWin()
    {
        var (mods, main) = KeyParser.Parse("Win+E");
        Assert.Single(mods, VirtualKeyShort.LWIN);
        Assert.Equal(VirtualKeyShort.KEY_E, main);
    }

    /// <summary>"Windows+E" は LWin 修飾と KEY_E に解析される (Windows 別名)。</summary>
    [Fact]
    public void Parse_WindowsModifier_ResolvesToLWin()
    {
        var (mods, main) = KeyParser.Parse("Windows+E");
        Assert.Single(mods, VirtualKeyShort.LWIN);
        Assert.Equal(VirtualKeyShort.KEY_E, main);
    }

    /// <summary>"ControlOrMeta" は削除されたため <see cref="System.ArgumentException"/>。</summary>
    [Fact]
    public void Parse_ControlOrMeta_Throws()
    {
        Assert.Throws<System.ArgumentException>(() => KeyParser.Parse("ControlOrMeta+E"));
    }

    /// <summary>ParseSingle で "Win" を単一キーとして指定できる。</summary>
    [Fact]
    public void ParseSingle_Win_ReturnsLWin()
    {
        var key = KeyParser.ParseSingle("Win");
        Assert.Equal(VirtualKeyShort.LWIN, key);
    }

    /// <summary>ParseSingle で "Meta" を単一キーとして指定できる。</summary>
    [Fact]
    public void ParseSingle_Meta_ReturnsLWin()
    {
        var key = KeyParser.ParseSingle("Meta");
        Assert.Equal(VirtualKeyShort.LWIN, key);
    }
}
