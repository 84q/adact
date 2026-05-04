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
    /// 全件 ok のとき Success exit と true 行を返すことを確認する。
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

        var (rows, exit, error) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Null(error);
        Assert.Equal(2, rows.Count);
        Assert.Equal("s1", rows[0][0]);
        Assert.Equal("true", rows[0][1]);
        Assert.Null(rows[0][2]);
        Assert.Equal("s2", rows[1][0]);
        Assert.Equal("true", rows[1][1]);
        Assert.Null(rows[1][2]);
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

        var (rows, exit, error) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Null(error);
        Assert.Equal("s1", rows[0][0]);
        Assert.Equal("true", rows[0][1]);
        Assert.Null(rows[0][2]);
        Assert.Equal("s2", rows[1][0]);
        Assert.Equal("false", rows[1][1]);
        Assert.Equal("CLOSE_FAILED", rows[1][2]);
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

        var (rows, exit, error) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Null(error);
        var row = Assert.Single(rows);
        Assert.Equal("s2", row[0]);
        Assert.Equal("false", row[1]);
        Assert.Null(row[2]);
    }

    /// <summary>
    /// results が空配列のとき、空出力と Success exit を返すことを確認する。
    /// </summary>
    [Fact]
    public void FormatResults_EmptyArray_ReturnsEmptyAndExitZero()
    {
        var info = Parse("""{ "results": [] }""");

        var (rows, exit, error) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Null(error);
        Assert.Empty(rows);
    }

    /// <summary>
    /// results フィールド自体が不在のときは malformed response として INTERNAL_ERROR 相当になることを確認する。
    /// </summary>
    [Fact]
    public void FormatResults_MissingResultsField_ReturnsMalformedError()
    {
        var info = Parse("""{ }""");

        var (rows, exit, error) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Empty(rows);
        Assert.Contains("results", error);
    }

    /// <summary>
    /// results が配列でないときも malformed response として扱うことを確認する。
    /// </summary>
    [Fact]
    public void FormatResults_NonArrayResults_ReturnsMalformedError()
    {
        var info = Parse("""{ "results": {} }""");

        var (rows, exit, error) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Empty(rows);
        Assert.Contains("results", error);
    }
}
