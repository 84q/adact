using Adact.Tests.Common;

using Xunit;

namespace Adact.Mcp.Http.Tests;

/// <summary>
/// Verifies external daemon URL resolution for HTTP MCP smoke / E2E fixtures.
/// </summary>
[Trait("Layer", "Unit")]
public class AdactHttpServerFixtureTests
{
    /// <summary>Unset environment variable keeps the default in-process server behavior.</summary>
    [Fact]
    public void GetExternalServerUri_WhenUnset_ReturnsNull()
    {
        Assert.Null(AdactHttpServerFixture.GetExternalServerUri(_ => null));
    }

    /// <summary>A configured HTTP MCP endpoint is trimmed and returned as the fixture base URL.</summary>
    [Fact]
    public void GetExternalServerUri_WhenSet_ReturnsTrimmedAbsoluteUrl()
    {
        Assert.Equal(
            new Uri("http://127.0.0.1:41300/mcp"),
            AdactHttpServerFixture.GetExternalServerUri(_ => "  http://127.0.0.1:41300/mcp  "));
    }

    /// <summary>Invalid endpoint values fail fast with a message that names the environment variable.</summary>
    [Theory]
    [InlineData("127.0.0.1:41300/mcp")]
    [InlineData("ftp://127.0.0.1:41300/mcp")]
    [InlineData("not a url")]
    public void GetExternalServerUri_WhenInvalid_Throws(string value)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => AdactHttpServerFixture.GetExternalServerUri(_ => value));
        Assert.Contains(ExternalServerHelper.ServerUrlEnvironmentVariable, ex.Message, StringComparison.Ordinal);
    }
}
