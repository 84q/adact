using Adact.Cli.Connection;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Config Loader behavior.</summary>
[Trait("Layer", "Unit")]
public class ConfigLoaderTests : IDisposable
{
    private readonly string _tempRoot;

    /// <summary>Initializes a new instance of the Config Loader Tests class.</summary>
    public ConfigLoaderTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "adact-cli-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    /// <summary>Releases resources.</summary>
    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { }
        GC.SuppressFinalize(this);
    }

    private static void WriteConfig(string dir, string json)
    {
        var adact = Path.Combine(dir, ".adact");
        Directory.CreateDirectory(adact);
        File.WriteAllText(Path.Combine(adact, "config.json"), json);
    }

    /// <summary>Performs the Find Server From Config Reads Server From Cwd operation.</summary>
    [Fact]
    public void FindServerFromConfig_ReadsServerFromCwd()
    {
        WriteConfig(_tempRoot, """{ "server": "http://1.2.3.4:41300/mcp" }""");

        var result = ConfigLoader.FindServerFromConfig(_tempRoot);

        Assert.Equal("http://1.2.3.4:41300/mcp", result);
    }

    /// <summary>Performs the Find Server From Config Finds Parent Config operation.</summary>
    [Fact]
    public void FindServerFromConfig_FindsParentConfig()
    {
        WriteConfig(_tempRoot, """{ "server": "http://parent:41300/mcp" }""");
        var child = Path.Combine(_tempRoot, "sub", "deep");
        Directory.CreateDirectory(child);

        var result = ConfigLoader.FindServerFromConfig(child);

        Assert.Equal("http://parent:41300/mcp", result);
    }

    /// <summary>Performs the Find Server From Config Returns Null When No Adact Dir operation.</summary>
    [Fact]
    public void FindServerFromConfig_ReturnsNullWhenNoAdactDir()
    {
        var result = ConfigLoader.FindServerFromConfig(_tempRoot);
        Assert.Null(result);
    }

    /// <summary>Performs the Find Server From Config Returns Null When Server Field Missing operation.</summary>
    [Fact]
    public void FindServerFromConfig_ReturnsNullWhenServerFieldMissing()
    {
        WriteConfig(_tempRoot, "{}");
        Assert.Null(ConfigLoader.FindServerFromConfig(_tempRoot));
    }

    /// <summary>Performs the Find Server From Config Returns Null When Server Empty operation.</summary>
    [Fact]
    public void FindServerFromConfig_ReturnsNullWhenServerEmpty()
    {
        WriteConfig(_tempRoot, """{ "server": "" }""");
        Assert.Null(ConfigLoader.FindServerFromConfig(_tempRoot));
    }

    /// <summary>Performs the Find Server From Config Throws On Invalid Json operation.</summary>
    [Fact]
    public void FindServerFromConfig_ThrowsOnInvalidJson()
    {
        WriteConfig(_tempRoot, "{ this is not json");

        Assert.Throws<ConfigParseException>(() => ConfigLoader.FindServerFromConfig(_tempRoot));
    }

    /// <summary>Performs the Find Server From Config Stops At First Adact Dir operation.</summary>
    [Fact]
    public void FindServerFromConfig_StopsAtFirstAdactDir()
    {
        WriteConfig(_tempRoot, """{ "server": "http://parent:41300/mcp" }""");
        var child = Path.Combine(_tempRoot, "sub");
        Directory.CreateDirectory(Path.Combine(child, ".adact"));

        var result = ConfigLoader.FindServerFromConfig(child);
        Assert.Null(result);
    }
}
