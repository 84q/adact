using Adact.Cli.Connection;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="ConfigLoader.FindServerFromConfig"/> の .adact/config.json 探索ロジックを検証する Unit テスト。
/// cwd とその親方向探索・server フィールド不在・不正 JSON の振る舞いの回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class ConfigLoaderTests : IDisposable
{
    private readonly string _tempRoot;

    /// <summary>テスト用一時ディレクトリを GUID 付きで作成する。</summary>
    public ConfigLoaderTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "adact-cli-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    /// <summary>テスト終了時に一時ディレクトリを再帰削除する。</summary>
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

    /// <summary>cwd 直下の .adact/config.json から server フィールドを読み取ることを確認する。</summary>
    [Fact]
    public void FindServerFromConfig_ReadsServerFromCwd()
    {
        WriteConfig(_tempRoot, """{ "server": "http://1.2.3.4:41300/mcp" }""");

        var result = ConfigLoader.FindServerFromConfig(_tempRoot);

        Assert.Equal("http://1.2.3.4:41300/mcp", result);
    }

    /// <summary>cwd に見つからないとき親方向へ .adact を探索して見つけることを確認する (全体の探索仕様の回帰防止)。</summary>
    [Fact]
    public void FindServerFromConfig_FindsParentConfig()
    {
        WriteConfig(_tempRoot, """{ "server": "http://parent:41300/mcp" }""");
        var child = Path.Combine(_tempRoot, "sub", "deep");
        Directory.CreateDirectory(child);

        var result = ConfigLoader.FindServerFromConfig(child);

        Assert.Equal("http://parent:41300/mcp", result);
    }

    /// <summary>.adact ディレクトリが全く見つからないとき null を返すことを確認する。</summary>
    [Fact]
    public void FindServerFromConfig_ReturnsNullWhenNoAdactDir()
    {
        // _tempRoot 配下に .adact/ なし。親方向にも (テンポラリパス上) 存在する想定はないのでルートまで遡って null。
        var result = ConfigLoader.FindServerFromConfig(_tempRoot);
        Assert.Null(result);
    }

    /// <summary>config.json が存在しても server フィールドが無いとき null を返すことを確認する。</summary>
    [Fact]
    public void FindServerFromConfig_ReturnsNullWhenServerFieldMissing()
    {
        WriteConfig(_tempRoot, "{}");
        Assert.Null(ConfigLoader.FindServerFromConfig(_tempRoot));
    }

    /// <summary>server が空文字列のとき null を返し、不正なデフォルト処理として処理されることを確認する。</summary>
    [Fact]
    public void FindServerFromConfig_ReturnsNullWhenServerEmpty()
    {
        WriteConfig(_tempRoot, """{ "server": "" }""");
        Assert.Null(ConfigLoader.FindServerFromConfig(_tempRoot));
    }

    /// <summary>不正 JSON は ConfigParseException として伝播され、黙って null にならないことを確認する。</summary>
    [Fact]
    public void FindServerFromConfig_ThrowsOnInvalidJson()
    {
        WriteConfig(_tempRoot, "{ this is not json");

        Assert.Throws<ConfigParseException>(() => ConfigLoader.FindServerFromConfig(_tempRoot));
    }

    /// <summary>最初に見つけた .adact/ で探索を打ち切り、そこに config.json が無い場合は null を返すことを確認する (親方向探索をさらに遡らない)。</summary>
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
