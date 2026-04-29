using Adact.Cli.Connection;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class ConfigLoaderTests : IDisposable
{
    private readonly string _tempRoot;

    public ConfigLoaderTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "adact-cli-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

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

    [Fact]
    public void FindServerFromConfig_ReadsServerFromCwd()
    {
        WriteConfig(_tempRoot, """{ "server": "http://1.2.3.4:41300/mcp" }""");

        var result = ConfigLoader.FindServerFromConfig(_tempRoot);

        Assert.Equal("http://1.2.3.4:41300/mcp", result);
    }

    [Fact]
    public void FindServerFromConfig_FindsParentConfig()
    {
        WriteConfig(_tempRoot, """{ "server": "http://parent:41300/mcp" }""");
        var child = Path.Combine(_tempRoot, "sub", "deep");
        Directory.CreateDirectory(child);

        var result = ConfigLoader.FindServerFromConfig(child);

        Assert.Equal("http://parent:41300/mcp", result);
    }

    [Fact]
    public void FindServerFromConfig_ReturnsNullWhenNoAdactDir()
    {
        // _tempRoot 配下に .adact/ なし。親方向にも (テンポラリ路径上) 存在する想定はないのでルートまで遡って null。
        var result = ConfigLoader.FindServerFromConfig(_tempRoot);
        Assert.Null(result);
    }

    [Fact]
    public void FindServerFromConfig_ReturnsNullWhenServerFieldMissing()
    {
        WriteConfig(_tempRoot, "{}");
        Assert.Null(ConfigLoader.FindServerFromConfig(_tempRoot));
    }

    [Fact]
    public void FindServerFromConfig_ReturnsNullWhenServerEmpty()
    {
        WriteConfig(_tempRoot, """{ "server": "" }""");
        Assert.Null(ConfigLoader.FindServerFromConfig(_tempRoot));
    }

    [Fact]
    public void FindServerFromConfig_ThrowsOnInvalidJson()
    {
        WriteConfig(_tempRoot, "{ this is not json");

        Assert.Throws<ConfigParseException>(() => ConfigLoader.FindServerFromConfig(_tempRoot));
    }

    [Fact]
    public void FindServerFromConfig_StopsAtFirstAdactDir()
    {
        // 親に .adact/config.json (server あり) を置きつつ、子に .adact/ ディレクトリだけ作成 (config.json なし)。
        // 子から遡る場合、最初に .adact/ を見つけた段階で停止 → null になる。
        WriteConfig(_tempRoot, """{ "server": "http://parent:41300/mcp" }""");
        var child = Path.Combine(_tempRoot, "sub");
        Directory.CreateDirectory(Path.Combine(child, ".adact"));

        var result = ConfigLoader.FindServerFromConfig(child);
        Assert.Null(result);
    }
}
