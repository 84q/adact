using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Tsv Writer behavior.</summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class TsvWriterTests
{
    /// <summary>Performs the Write Header Joins Columns With Tab operation.</summary>
    [Fact]
    public void WriteHeader_JoinsColumnsWithTab()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            TsvWriter.WriteHeader("a", "b", "c");
        });

        Assert.Equal("a\tb\tc" + Environment.NewLine, stdout);
    }

    /// <summary>Performs the Write Row Joins Cells With Tab operation.</summary>
    [Fact]
    public void WriteRow_JoinsCellsWithTab()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
        });

    }

    /// <summary>Performs the Write Row Null Or Empty Rendered As Dash operation.</summary>
    [Fact]
    public void WriteRow_NullOrEmpty_RenderedAsDash()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
        });

    }
}
