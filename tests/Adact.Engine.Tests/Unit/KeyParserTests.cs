using FlaUI.Core.WindowsAPI;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Key Parser behavior.</summary>
[Trait("Layer", "Unit")]
public class KeyParserTests
{
    /// <summary>Performs the Parse Letter Only Returns Key Enum operation.</summary>
    [Fact]
    public void Parse_LetterOnly_ReturnsKeyEnum()
    {
        var (mods, main) = KeyParser.Parse("A");
        Assert.Empty(mods);
        Assert.Equal(VirtualKeyShort.KEY_A, main);
    }

    /// <summary>Performs the Parse Ctrl Plus Letter Returns Modifier And Main operation.</summary>
    [Fact]
    public void Parse_CtrlPlusLetter_ReturnsModifierAndMain()
    {
        var (mods, main) = KeyParser.Parse("Ctrl+S");
        Assert.Single(mods, VirtualKeyShort.CONTROL);
        Assert.Equal(VirtualKeyShort.KEY_S, main);
    }

    /// <summary>Performs the Parse Function Key Resolves operation.</summary>
    [Fact]
    public void Parse_FunctionKey_Resolves()
    {
        var (_, main) = KeyParser.Parse("F5");
        Assert.Equal(VirtualKeyShort.F5, main);
    }

    /// <summary>Performs the Parse Unknown Key Throws operation.</summary>
    [Fact]
    public void Parse_UnknownKey_Throws()
    {
        Assert.Throws<System.ArgumentException>(() => KeyParser.Parse("Foo"));
    }

    /// <summary>Performs the Parse Single Rejects Combo operation.</summary>
    [Fact]
    public void ParseSingle_RejectsCombo()
    {
        Assert.Throws<System.ArgumentException>(() => KeyParser.ParseSingle("Ctrl+A"));
    }

    /// <summary>Performs the Parse Meta Modifier Resolves To LWin operation.</summary>
    [Fact]
    public void Parse_MetaModifier_ResolvesToLWin()
    {
        var (mods, main) = KeyParser.Parse("Meta+E");
        Assert.Single(mods, VirtualKeyShort.LWIN);
        Assert.Equal(VirtualKeyShort.KEY_E, main);
    }

    /// <summary>Performs the Parse Win Modifier Resolves To LWin operation.</summary>
    [Fact]
    public void Parse_WinModifier_ResolvesToLWin()
    {
        var (mods, main) = KeyParser.Parse("Win+E");
        Assert.Single(mods, VirtualKeyShort.LWIN);
        Assert.Equal(VirtualKeyShort.KEY_E, main);
    }

    /// <summary>Performs the Parse Windows Modifier Resolves To LWin operation.</summary>
    [Fact]
    public void Parse_WindowsModifier_ResolvesToLWin()
    {
        var (mods, main) = KeyParser.Parse("Windows+E");
        Assert.Single(mods, VirtualKeyShort.LWIN);
        Assert.Equal(VirtualKeyShort.KEY_E, main);
    }

    /// <summary>Performs the Parse Control Or Meta Throws operation.</summary>
    [Fact]
    public void Parse_ControlOrMeta_Throws()
    {
        Assert.Throws<System.ArgumentException>(() => KeyParser.Parse("ControlOrMeta+E"));
    }

    /// <summary>Performs the Parse Single Win Returns LWin operation.</summary>
    [Fact]
    public void ParseSingle_Win_ReturnsLWin()
    {
        var key = KeyParser.ParseSingle("Win");
        Assert.Equal(VirtualKeyShort.LWIN, key);
    }

    /// <summary>Performs the Parse Single Meta Returns LWin operation.</summary>
    [Fact]
    public void ParseSingle_Meta_ReturnsLWin()
    {
        var key = KeyParser.ParseSingle("Meta");
        Assert.Equal(VirtualKeyShort.LWIN, key);
    }
}
