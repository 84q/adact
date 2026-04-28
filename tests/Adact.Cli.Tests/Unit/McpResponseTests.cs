using System.Text.Json;

using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class McpResponseTests
{
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

  [Fact]
  public void TryReportError_OnSuccess_ReturnsNull()
  {
    var result = new CallToolResult
    {
      Content = [new TextContentBlock { Text = "{}" }],
    };

    Assert.Null(McpResponse.TryReportError(result));
  }

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
