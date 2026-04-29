using Adact.Cli.Connection;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="ServerEndpoint.Parse"/> の URL パースと IsLocalhost フラグ判定を検証する Unit テスト。
/// daemon-stop の localhost ガードを支えるパーサ仕様の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class ServerEndpointTests
{
    /// <summary>127.0.0.1 / localhost (大文字含む) / [::1] は IsLocalhost=true、それ以外のホストは false となることを確認する。</summary>
    /// <param name="url">検証対象 URL。</param>
    /// <param name="expectedLocalhost">IsLocalhost の期待値。</param>
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

    /// <summary>https スキームも受け付け、Url.Scheme が保存されることを確認する。</summary>
    [Fact]
    public void Parse_HttpsUrl_Accepted()
    {
        var ep = ServerEndpoint.Parse("https://example.com:8443/mcp");
        Assert.Equal("https", ep.Url.Scheme);
    }

    /// <summary>スキーム無しのホスト名だけや host:port は InvalidUrlException として拒否されることを確認する。</summary>
    /// <param name="url">検証対象の不正入力。</param>
    [Theory]
    [InlineData("192.168.1.10")]
    [InlineData("localhost")]
    [InlineData("127.0.0.1:41300")]
    public void Parse_HostOnly_Throws(string url)
    {
        Assert.Throws<InvalidUrlException>(() => ServerEndpoint.Parse(url));
    }

    /// <summary>http/https 以外のスキーム (ftp etc.) は InvalidUrlException として拒否されることを確認する。</summary>
    [Fact]
    public void Parse_NonHttpScheme_Throws()
    {
        Assert.Throws<InvalidUrlException>(() => ServerEndpoint.Parse("ftp://example.com:80/"));
    }

    /// <summary>空文字列は InvalidUrlException として拒否されることを確認する。</summary>
    [Fact]
    public void Parse_EmptyString_Throws()
    {
        Assert.Throws<InvalidUrlException>(() => ServerEndpoint.Parse(""));
    }

    /// <summary>null は InvalidUrlException として拒否されることを確認する。</summary>
    [Fact]
    public void Parse_NullString_Throws()
    {
        Assert.Throws<InvalidUrlException>(() => ServerEndpoint.Parse(null!));
    }

    /// <summary>不正形式の任意文字列は InvalidUrlException として拒否されることを確認する。</summary>
    [Fact]
    public void Parse_GarbageString_Throws()
    {
        Assert.Throws<InvalidUrlException>(() => ServerEndpoint.Parse("not a url"));
    }
}
