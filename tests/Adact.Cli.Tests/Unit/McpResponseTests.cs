using System.Text.Json;

using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Mcp Response behavior.</summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class McpResponseTests
{
    /// <summary>Gets the Get Json Prefers Structured Content value.</summary>
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

    /// <summary>Gets the Get Json Falls Back To Content Text When Structured Absent value.</summary>
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

    /// <summary>Attempts to perform the Try Report Error On Success Returns Null operation.</summary>
    [Fact]
    public void TryReportError_OnSuccess_ReturnsNull()
    {
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "{}" }],
        };

        Assert.Null(McpResponse.TryReportError(result));
    }

    /// <summary>Attempts to perform the Try Report Error On Error Writes Stderr And Returns Exit Code operation.</summary>
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

        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: AMBIGUOUS_ATTACH", stdout);
        Assert.Contains("message: two windows match", stdout);
    }

    /// <summary>Attempts to perform the Try Report Error On Error Without Structured Falls Back To Text operation.</summary>
    [Fact]
    public void TryReportError_OnError_WithoutStructured_FallsBackToText()
    {
        var result = new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = "raw error text" }],
        };

        var (stdout, stderr) = CapturedConsole.Run(() =>
        {
            var exit = McpResponse.TryReportError(result);
            Assert.Equal(ExitCodes.CommandFailed, exit);
        });

        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: INTERNAL_ERROR", stdout);
        Assert.Contains("raw error text", stdout);
    }

    /// <summary>Attempts to perform the Try Report Error On Error With Hint Writes Hint Line operation.</summary>
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

        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: STALE_REF", stdout);
        Assert.Contains("message: ref not found in current snapshot", stdout);
        Assert.Contains("hint: rerun snapshot", stdout);
    }
}
