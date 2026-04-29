using System.Text.Json;

using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="McpResponse"/> の structured/text content 処理とエラー出力 (stderr) を検証する Unit テスト。
/// errors-and-output.md の CLI エラー表示仕様 (error / message / hint) の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class McpResponseTests
{
    /// <summary>StructuredContent があるときは Content.Text より structured を優先することを確認する。</summary>
    [Fact]
    public void GetJson_PrefersStructuredContent()
    {
        var structured = JsonSerializer.SerializeToElement(new { foo = "bar" });
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "ignored" }],
            StructuredContent = structured,
        };

        var json = McpResponse.GetJson(result);

        Assert.Equal(JsonValueKind.Object, json.ValueKind);
        Assert.Equal("bar", json.GetProperty("foo").GetString());
    }

    /// <summary>StructuredContent が無いとき Content.Text の JSON 文字列をパースして出力することを確認する (フォールバックパスの回帰防止)。</summary>
    [Fact]
    public void GetJson_FallsBackToContentText_WhenStructuredAbsent()
    {
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = """[{"a":1}]""" }],
        };

        var json = McpResponse.GetJson(result);

        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        Assert.Equal(1, json[0].GetProperty("a").GetInt32());
    }

    /// <summary>IsError が未設定の成功レスポンスでは null を返し、エラー出力を行わないことを確認する。</summary>
    [Fact]
    public void TryReportError_OnSuccess_ReturnsNull()
    {
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "{}" }],
        };

        Assert.Null(McpResponse.TryReportError(result));
    }

    /// <summary>IsError=true のとき structured の code/message を stderr に error/message 行として出し、CommandFailed exit を返すことを確認する。</summary>
    [Fact]
    public void TryReportError_OnError_WritesStderrAndReturnsExitCode()
    {
        var structured = JsonSerializer.SerializeToElement(new
        {
            code = "AMBIGUOUS_ATTACH",
            message = "two windows match",
        });
        var result = new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = "AMBIGUOUS_ATTACH: two windows match" }],
            StructuredContent = structured,
        };

        var (stdout, stderr) = CapturedConsole.Run(() =>
        {
            var exit = McpResponse.TryReportError(result);
            Assert.Equal(ExitCodes.CommandFailed, exit);
        });

        Assert.Equal(string.Empty, stdout);
        Assert.Contains("error AMBIGUOUS_ATTACH", stderr);
        Assert.Contains("message two windows match", stderr);
    }

    /// <summary>structured が無いエラーレスポンスでは INTERNAL_ERROR として raw text を表示することを確認する。</summary>
    [Fact]
    public void TryReportError_OnError_WithoutStructured_FallsBackToText()
    {
        var result = new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = "raw error text" }],
        };

        var (_, stderr) = CapturedConsole.Run(() =>
        {
            var exit = McpResponse.TryReportError(result);
            Assert.Equal(ExitCodes.CommandFailed, exit);
        });

        Assert.Contains("error INTERNAL_ERROR", stderr);
        Assert.Contains("raw error text", stderr);
    }

    /// <summary>structured に hint があるとき stderr に hint 行も出力されることを確認する。</summary>
    [Fact]
    public void TryReportError_OnError_WithHint_WritesHintLine()
    {
        var structured = JsonSerializer.SerializeToElement(new
        {
            code = "STALE_REF",
            message = "ref not found in current snapshot",
            hint = "rerun snapshot",
        });
        var result = new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = "STALE_REF: stale" }],
            StructuredContent = structured,
        };

        var (stdout, stderr) = CapturedConsole.Run(() =>
        {
            var exit = McpResponse.TryReportError(result);
            Assert.Equal(ExitCodes.CommandFailed, exit);
        });

        Assert.Equal(string.Empty, stdout);
        Assert.Contains("error STALE_REF", stderr);
        Assert.Contains("message ref not found in current snapshot", stderr);
        Assert.Contains("hint rerun snapshot", stderr);
    }
}
