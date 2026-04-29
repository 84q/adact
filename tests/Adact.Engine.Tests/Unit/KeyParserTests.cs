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
}
