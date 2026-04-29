using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="TsvWriter"/> のタブ区切りヘッダ・行出力と null/空のダッシュ代替を検証する Unit テスト。
/// list-apps 出力フォーマットの回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class TsvWriterTests
{
    /// <summary>WriteHeader が任意個数の列をタブで連結し、末尾に改行を付けることを確認する。</summary>
    [Fact]
    public void WriteHeader_JoinsColumnsWithTab()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            TsvWriter.WriteHeader("a", "b", "c");
        });

        Assert.Equal("a\tb\tc" + Environment.NewLine, stdout);
    }

    /// <summary>WriteRow がセルをタブで連結し、日本語を含む値もそのまま出すことを確認する。</summary>
    [Fact]
    public void WriteRow_JoinsCellsWithTab()
    {
        var (stdout, _) = CapturedConsole.Run(() =>
        {
            TsvWriter.WriteRow("w1", "s1", "calc.exe", "1234", "Frame", "電卓");
        });

        Assert.Equal("w1\ts1\tcalc.exe\t1234\tFrame\t電卓" + Environment.NewLine, stdout);
    }

    /// <summary>null / 空文字列セルが "-" として描画され、TSV のカラム数を保つことを確認する。</summary>
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
