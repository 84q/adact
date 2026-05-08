using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Adact Daemon Fixture behavior.</summary>
[Trait("Layer", "Unit")]
public class AdactDaemonFixtureTests
{
    /// <summary>Gets the Get External Server Url When Unset Returns Null value.</summary>
    [Fact]
    public void GetExternalServerUrl_WhenUnset_ReturnsNull()
    {
        Assert.Null(AdactDaemonFixture.GetExternalServerUrl(_ => null));
    }

    /// <summary>Gets the Get External Server Url When Set Returns Trimmed Absolute Url value.</summary>
    [Fact]
    public void GetExternalServerUrl_WhenSet_ReturnsTrimmedAbsoluteUrl()
    {
        Assert.Equal(
            "http://127.0.0.1:41300/mcp",
            AdactDaemonFixture.GetExternalServerUrl(_ => "  http://127.0.0.1:41300/mcp  "));
    }

    /// <summary>Gets the Get External Server Url When Invalid Throws value.</summary>
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
