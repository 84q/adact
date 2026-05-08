using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Key Value Writer behavior.</summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class KeyValueWriterTests
{
    /// <summary>Performs the Write Basic Key Value Outputs Key Space Value operation.</summary>
    [Fact]
    public void Write_BasicKeyValue_OutputsKeySpaceValue()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            KeyValueWriter.Write("name", "hello");
        });

        Assert.Equal("name hello" + Environment.NewLine, stdout);
    }

    /// <summary>Performs the Write Value With Spaces Preserves Spaces operation.</summary>
    [Fact]
    public void Write_ValueWithSpaces_PreservesSpaces()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            KeyValueWriter.Write("title", "hello world foo");
        });

        Assert.Equal("title hello world foo" + Environment.NewLine, stdout);
    }

    /// <summary>Performs the Write Value With Unicode Preserves Characters operation.</summary>
    [Fact]
    public void Write_ValueWithUnicode_PreservesCharacters()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
        });

    }

    /// <summary>Performs the Write Empty Value Outputs Key With Trailing Space operation.</summary>
    [Fact]
    public void Write_EmptyValue_OutputsKeyWithTrailingSpace()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            KeyValueWriter.Write("key", "");
        });

        Assert.Equal("key " + Environment.NewLine, stdout);
    }

    /// <summary>Performs the Write Value With Special Characters Outputs As Is operation.</summary>
    [Fact]
    public void Write_ValueWithSpecialCharacters_OutputsAsIs()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            KeyValueWriter.Write("data", "a\tb");
        });

        Assert.Equal("data a\tb" + Environment.NewLine, stdout);
    }
}
