using Adact.Cli.Connection;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class ConnectionResolverTests : IDisposable
{
  private readonly string _tempRoot;

  public ConnectionResolverTests()
  {
    _tempRoot = Path.Combine(
        Path.GetTempPath(),
        "adact-resolver-tests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_tempRoot);
  }

  public void Dispose()
  {
    try { Directory.Delete(_tempRoot, recursive: true); } catch { }
    GC.SuppressFinalize(this);
  }

  private void WriteConfig(string json)
  {
    var adact = Path.Combine(_tempRoot, ".adact");
    Directory.CreateDirectory(adact);
    File.WriteAllText(Path.Combine(adact, "config.json"), json);
  }

  [Fact]
  public void Resolve_ExplicitServer_TakesPrecedenceOverConfig()
  {
    WriteConfig("""{ "server": "http://from-config:41300/mcp" }""");

    var ep = ConnectionResolver.Resolve("http://explicit:41300/mcp", _tempRoot);

    Assert.Equal("http://explicit:41300/mcp", ep.Url.ToString().TrimEnd('/'));
  }

  [Fact]
  public void Resolve_NoExplicit_UsesConfig()
  {
    WriteConfig("""{ "server": "http://from-config:41300/mcp" }""");

    var ep = ConnectionResolver.Resolve(null, _tempRoot);

    Assert.Equal("http://from-config:41300/mcp", ep.Url.ToString().TrimEnd('/'));
  }

  [Fact]
  public void Resolve_NoExplicitNoConfig_UsesDefault()
  {
    // _tempRoot 配下に .adact/ なし。親方向にも (CI / 開発環境共に) 通常存在しない想定。
    var ep = ConnectionResolver.Resolve(null, _tempRoot);

    Assert.Equal(ConnectionResolver.DefaultUrl, ep.Url.ToString().TrimEnd('/'));
    Assert.True(ep.IsLocalhost);
  }

  [Fact]
  public void Resolve_EmptyExplicit_FallsBackToConfig()
  {
    WriteConfig("""{ "server": "http://from-config:41300/mcp" }""");

    var ep = ConnectionResolver.Resolve("   ", _tempRoot);

    Assert.Equal("http://from-config:41300/mcp", ep.Url.ToString().TrimEnd('/'));
  }

  [Fact]
  public void Resolve_InvalidExplicit_Throws()
  {
    Assert.Throws<InvalidUrlException>(() => ConnectionResolver.Resolve("not-a-url", _tempRoot));
  }

  [Fact]
  public void Resolve_InvalidConfig_Throws()
  {
    WriteConfig("""{ "server": "not-a-url" }""");

    Assert.Throws<InvalidUrlException>(() => ConnectionResolver.Resolve(null, _tempRoot));
  }
}
