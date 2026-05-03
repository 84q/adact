using Adact.Cli.Connection;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="ConnectionResolver.ResolveHttpEndpoint"/> の --server / config.json / デフォルトの優先順位・エラー伝播を検証する Unit テスト。
/// CLI 接続先解決仕様 (cli.md §--server) の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class ConnectionResolverTests : IDisposable
{
    private readonly string _tempRoot;

    /// <summary>テスト用一時ディレクトリを作成する。</summary>
    public ConnectionResolverTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "adact-resolver-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    /// <summary>テスト終了時に一時ディレクトリを再帰削除する。</summary>
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

    /// <summary>--server 明示指定が config.json よりも優先されることを確認する。</summary>
    [Fact]
    public void Resolve_ExplicitServer_TakesPrecedenceOverConfig()
    {
        WriteConfig("""{ "server": "http://from-config:41300/mcp" }""");

        var ep = ConnectionResolver.ResolveHttpEndpoint("http://explicit:41300/mcp", _tempRoot);

        Assert.NotNull(ep);
        Assert.Equal("http://explicit:41300/mcp", ep.Url.ToString().TrimEnd('/'));
    }

    /// <summary>--server 未指定のとき config.json の server を使用することを確認する。</summary>
    [Fact]
    public void Resolve_NoExplicit_UsesConfig()
    {
        WriteConfig("""{ "server": "http://from-config:41300/mcp" }""");

        var ep = ConnectionResolver.ResolveHttpEndpoint(null, _tempRoot);

        Assert.NotNull(ep);
        Assert.Equal("http://from-config:41300/mcp", ep.Url.ToString().TrimEnd('/'));
    }

    /// <summary>--server も config も無いとき HTTP エンドポイントは解決されず null が返される（Named Pipe モード）。</summary>
    [Fact]
    public void Resolve_NoExplicitNoConfig_ReturnsNullForHttpMode()
    {
        // _tempRoot 配下に .adact/ なし。親方向にも (CI / 開発環境共に) 通常存在しない想定。
        var ep = ConnectionResolver.ResolveHttpEndpoint(null, _tempRoot);

        // --server未指定かつconfig.json未設定時は、HTTPではなくNamed Pipeモードを使用するためnullが返される
        Assert.Null(ep);
    }

    /// <summary>--server も config も無いとき Named Pipe エンドポイントが取得できることを確認する。</summary>
    [Fact]
    public void Resolve_NoExplicitNoConfig_ProvidesNamedPipeEndpoint()
    {
        // Named Pipe エンドポイントは常に取得可能
        var pipeEp = ConnectionResolver.ResolveNamedPipeEndpoint(_tempRoot);

        Assert.NotNull(pipeEp);
        Assert.False(string.IsNullOrEmpty(pipeEp.PipeName));
    }

    /// <summary>--server が空白のみという未指定相当のとき config.json にフォールバックすることを確認する。</summary>
    [Fact]
    public void Resolve_EmptyExplicit_FallsBackToConfig()
    {
        WriteConfig("""{ "server": "http://from-config:41300/mcp" }""");

        var ep = ConnectionResolver.ResolveHttpEndpoint("   ", _tempRoot);

        Assert.NotNull(ep);
        Assert.Equal("http://from-config:41300/mcp", ep.Url.ToString().TrimEnd('/'));
    }

    /// <summary>明示指定された --server が不正な URL のとき InvalidUrlException を伝播することを確認する。</summary>
    [Fact]
    public void Resolve_InvalidExplicit_Throws()
    {
        Assert.Throws<InvalidUrlException>(() => ConnectionResolver.ResolveHttpEndpoint("not-a-url", _tempRoot));
    }

    /// <summary>config.json 側の server が不正 URL のときも InvalidUrlException を伝播することを確認する。</summary>
    [Fact]
    public void Resolve_InvalidConfig_Throws()
    {
        WriteConfig("""{ "server": "not-a-url" }""");

        Assert.Throws<InvalidUrlException>(() => ConnectionResolver.ResolveHttpEndpoint(null, _tempRoot));
    }
}
