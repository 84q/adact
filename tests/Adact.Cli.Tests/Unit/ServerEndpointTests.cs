using Adact.Cli.Connection;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class ServerEndpointTests
{
  [Theory]
  [InlineData("http://127.0.0.1:41300/mcp", true)]
  [InlineData("http://localhost:41300/mcp", true)]
  [InlineData("http://LocalHost:41300/mcp", true)]
  [InlineData("http://[::1]:41300/mcp", true)]
  [InlineData("http://192.168.1.10:41300/mcp", false)]
  [InlineData("https://example.com/mcp", false)]
  public void Parse_ValidUrl_SetsLocalhostFlagCorrectly(string url, bool expectedLocalhost)
  {
    var ep = ServerEndpoint.Parse(url);

    Assert.Equal(expectedLocalhost, ep.IsLocalhost);
    Assert.NotNull(ep.Url);
  }

  [Fact]
  public void Parse_HttpsUrl_Accepted()
  {
    var ep = ServerEndpoint.Parse("https://example.com:8443/mcp");
    Assert.Equal("https", ep.Url.Scheme);
  }

  [Theory]
  [InlineData("192.168.1.10")]
  [InlineData("localhost")]
  [InlineData("127.0.0.1:41300")]
  public void Parse_HostOnly_Throws(string url)
  {
    Assert.Throws<InvalidUrlException>(() => ServerEndpoint.Parse(url));
  }

  [Fact]
  public void Parse_NonHttpScheme_Throws()
  {
    Assert.Throws<InvalidUrlException>(() => ServerEndpoint.Parse("ftp://example.com:80/"));
  }

  [Fact]
  public void Parse_EmptyString_Throws()
  {
    Assert.Throws<InvalidUrlException>(() => ServerEndpoint.Parse(""));
  }

  [Fact]
  public void Parse_NullString_Throws()
  {
    Assert.Throws<InvalidUrlException>(() => ServerEndpoint.Parse(null!));
  }

  [Fact]
  public void Parse_GarbageString_Throws()
  {
    Assert.Throws<InvalidUrlException>(() => ServerEndpoint.Parse("not a url"));
  }
}
