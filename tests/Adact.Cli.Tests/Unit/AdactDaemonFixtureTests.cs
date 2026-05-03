using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// Verifies external daemon URL resolution for CLI Smoke / E2E fixtures.
/// </summary>
[Trait("Layer", "Unit")]
public class AdactDaemonFixtureTests
{
    /// <summary>Unset environment variable keeps the default self-hosted daemon behavior.</summary>
    [Fact]
    public void GetExternalServerUrl_WhenUnset_ReturnsNull()
    {
        Assert.Null(AdactDaemonFixture.GetExternalServerUrl(_ => null));
    }

    /// <summary>A configured HTTP MCP endpoint is trimmed and returned as the fixture base URL.</summary>
    [Fact]
    public void GetExternalServerUrl_WhenSet_ReturnsTrimmedAbsoluteUrl()
    {
        Assert.Equal(
            "http://127.0.0.1:41300/mcp",
            AdactDaemonFixture.GetExternalServerUrl(_ => "  http://127.0.0.1:41300/mcp  "));
    }

    /// <summary>Invalid endpoint values fail fast with a message that names the environment variable.</summary>
    [Theory]
    [InlineData("127.0.0.1:41300/mcp")]
    [InlineData("ftp://127.0.0.1:41300/mcp")]
    [InlineData("not a url")]
    public void GetExternalServerUrl_WhenInvalid_Throws(string value)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => AdactDaemonFixture.GetExternalServerUrl(_ => value));
        Assert.Contains(AdactDaemonFixture.ServerUrlEnvironmentVariable, ex.Message, StringComparison.Ordinal);
    }
}
