using Adact.Cli.Connection;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Connection Resolver behavior.</summary>
[Trait("Layer", "Unit")]
public class ConnectionResolverTests : IDisposable
{
    private readonly string _tempRoot;

    /// <summary>Initializes a new instance of the Connection Resolver Tests class.</summary>
    public ConnectionResolverTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "adact-resolver-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    /// <summary>Releases resources.</summary>
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

    /// <summary>Resolves the Resolve Explicit Server Takes Precedence Over Config value.</summary>
    [Fact]
    public void Resolve_ExplicitServer_TakesPrecedenceOverConfig()
    {
        WriteConfig("""{ "server": "http://from-config:41300/mcp" }""");

        var ep = ConnectionResolver.ResolveHttpEndpoint("http://explicit:41300/mcp", _tempRoot);

        Assert.NotNull(ep);
        Assert.Equal("http://explicit:41300/mcp", ep.Url.ToString().TrimEnd('/'));
    }

    /// <summary>Resolves the Resolve No Explicit Uses Config value.</summary>
    [Fact]
    public void Resolve_NoExplicit_UsesConfig()
    {
        WriteConfig("""{ "server": "http://from-config:41300/mcp" }""");

        var ep = ConnectionResolver.ResolveHttpEndpoint(null, _tempRoot);

        Assert.NotNull(ep);
        Assert.Equal("http://from-config:41300/mcp", ep.Url.ToString().TrimEnd('/'));
    }

    /// <summary>Resolves the Resolve No Explicit No Config Returns Null For Http Mode value.</summary>
    [Fact]
    public void Resolve_NoExplicitNoConfig_ReturnsNullForHttpMode()
    {
        var ep = ConnectionResolver.ResolveHttpEndpoint(null, _tempRoot);

        Assert.Null(ep);
    }

    /// <summary>Resolves the Resolve No Explicit No Config Provides Named Pipe Endpoint value.</summary>
    [Fact]
    public void Resolve_NoExplicitNoConfig_ProvidesNamedPipeEndpoint()
    {
        var pipeEp = ConnectionResolver.ResolveNamedPipeEndpoint(_tempRoot);

        Assert.NotNull(pipeEp);
        Assert.False(string.IsNullOrEmpty(pipeEp.PipeName));
    }

    /// <summary>Resolves the Resolve Empty Explicit Falls Back To Config value.</summary>
    [Fact]
    public void Resolve_EmptyExplicit_FallsBackToConfig()
    {
        WriteConfig("""{ "server": "http://from-config:41300/mcp" }""");

        var ep = ConnectionResolver.ResolveHttpEndpoint("   ", _tempRoot);

        Assert.NotNull(ep);
        Assert.Equal("http://from-config:41300/mcp", ep.Url.ToString().TrimEnd('/'));
    }

    /// <summary>Resolves the Resolve Invalid Explicit Throws value.</summary>
    [Fact]
    public void Resolve_InvalidExplicit_Throws()
    {
        Assert.Throws<InvalidUrlException>(() => ConnectionResolver.ResolveHttpEndpoint("not-a-url", _tempRoot));
    }

    /// <summary>Resolves the Resolve Invalid Config Throws value.</summary>
    [Fact]
    public void Resolve_InvalidConfig_Throws()
    {
        WriteConfig("""{ "server": "not-a-url" }""");

        Assert.Throws<InvalidUrlException>(() => ConnectionResolver.ResolveHttpEndpoint(null, _tempRoot));
    }
}
