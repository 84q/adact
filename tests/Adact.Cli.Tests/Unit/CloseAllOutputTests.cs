using System.Text.Json;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class CloseAllOutputTests
{
    private static JsonElement Parse(string json)
        => JsonSerializer.Deserialize<JsonElement>(json);

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

    [Fact]
    public void FormatResults_EmptyArray_ReturnsEmptyAndExitZero()
    {
        var info = Parse("""{ "results": [] }""");

        var (output, exit) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void FormatResults_MissingResultsField_ReturnsEmptyAndExitZero()
    {
        var info = Parse("""{ }""");

        var (output, exit) = CloseAllCommand.FormatResults(info);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, output);
    }
}
