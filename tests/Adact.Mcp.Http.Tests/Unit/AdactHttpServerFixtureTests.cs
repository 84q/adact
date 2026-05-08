using Adact.Tests.Common;

using Xunit;

namespace Adact.Mcp.Http.Tests;

/// <summary>Contains tests for the Adact Http Server Fixture behavior.</summary>
[Trait("Layer", "Unit")]
public class AdactHttpServerFixtureTests
{
    /// <summary>Gets the Get External Server Uri When Unset Returns Null value.</summary>
    [Fact]
    public void GetExternalServerUri_WhenUnset_ReturnsNull()
    {
        Assert.Null(AdactHttpServerFixture.GetExternalServerUri(_ => null));
    }

    /// <summary>Gets the Get External Server Uri When Set Returns Trimmed Absolute Url value.</summary>
    [Fact]
    public void GetExternalServerUri_WhenSet_ReturnsTrimmedAbsoluteUrl()
    {
        Assert.Equal(
            new Uri("http://127.0.0.1:41300/mcp"),
            AdactHttpServerFixture.GetExternalServerUri(_ => "  http://127.0.0.1:41300/mcp  "));
    }

    /// <summary>Gets the Get External Server Uri When Invalid Throws value.</summary>
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
