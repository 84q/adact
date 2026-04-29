using System.Text.Json;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="CloseAllCommand.FormatResults"/> の TSV 出力と exit code 判定を検証する Unit テスト。
/// close_all のさまざまな結果パターン (全成功 / 部分失敗 / 空 / fields 不在) の出力仕様の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class CloseAllOutputTests
{
    private static JsonElement Parse(string json)
        => JsonSerializer.Deserialize<JsonElement>(json);

    /// <summary>
    /// 全件 ok のとき Success exit と "sid\tok\n" 形式の TSV を返すことを確認する。
    /// </summary>
    [Fact]
    public void FormatResults_AllOk_ReturnsTsvAndExitZero()
    {
        var info = Parse("""
        {
          "results": [
            { "sessionId": "s1", "result": "ok" },
            { "sessionId": "s2", "result": "ok" }
          ]
        }
        """);

        var (output, exit) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("s1\tok\ns2\tok\n", output);
    }

    /// <summary>
    /// 一部が fail のとき CommandFailed exit と error コード付きの行を返すことを確認する。
    /// </summary>
    [Fact]
    public void FormatResults_PartialFail_IncludesErrorAndExitOne()
    {
        var info = Parse("""
        {
          "results": [
            { "sessionId": "s1", "result": "ok" },
            { "sessionId": "s2", "result": "fail", "error": "CLOSE_FAILED", "message": "boom" }
          ]
        }
        """);

        var (output, exit) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Contains("s1\tok\n", output);
        Assert.Contains("s2\tfail\tCLOSE_FAILED\n", output);
    }

    /// <summary>
    /// fail だが error フィールドが無いケースで、error カラムを出さず sid\tfail のみ出すことを確認する。
    /// </summary>
    [Fact]
    public void FormatResults_FailWithoutError_OmitsErrorColumn()
    {
        var info = Parse("""
        {
          "results": [
            { "sessionId": "s2", "result": "fail" }
          ]
        }
        """);

        var (output, exit) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Equal("s2\tfail\n", output);
    }

    /// <summary>
    /// results が空配列のとき、空出力と Success exit を返すことを確認する。
    /// </summary>
    [Fact]
    public void FormatResults_EmptyArray_ReturnsEmptyAndExitZero()
    {
        var info = Parse("""{ "results": [] }""");

        var (output, exit) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, output);
    }

    /// <summary>
    /// results フィールド自体が不在のときも、例外ではなく空出力と Success exit として扱うことを確認する。
    /// </summary>
    [Fact]
    public void FormatResults_MissingResultsField_ReturnsEmptyAndExitZero()
    {
        var info = Parse("""{ }""");

        var (output, exit) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, output);
    }
}
