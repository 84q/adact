using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class TsvWriterTests
{
    [Fact]
    public void WriteHeader_JoinsColumnsWithTab()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            TsvWriter.WriteHeader("a", "b", "c");
        });

        Assert.Equal("a\tb\tc" + Environment.NewLine, stdout);
    }

    [Fact]
    public void WriteRow_JoinsCellsWithTab()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            TsvWriter.WriteRow("w1", "s1", "calc.exe", "1234", "Frame", "電卓");
        });

        Assert.Equal("w1\ts1\tcalc.exe\t1234\tFrame\t電卓" + Environment.NewLine, stdout);
    }

    [Fact]
    public void WriteRow_NullOrEmpty_RenderedAsDash()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            TsvWriter.WriteRow("w2", null, "notepad.exe", "5678", "", "無題");
        });

        Assert.Equal("w2\t-\tnotepad.exe\t5678\t-\t無題" + Environment.NewLine, stdout);
    }
}
