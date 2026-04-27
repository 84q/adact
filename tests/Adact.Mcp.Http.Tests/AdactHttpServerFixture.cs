using Adact.Cli;

using Microsoft.AspNetCore.Builder;

using Xunit;

namespace Adact.Mcp.Http.Tests;

/// <summary>
/// HttpHost.BuildApplication(port:0) で実 Kestrel を起動し、ephemeral port を採番させる
/// xUnit フィクスチャ。テスト間でサーバーを再利用する (Stateless モードのため共有可能)。
/// </summary>
public sealed class AdactHttpServerFixture : IAsyncLifetime
{
    private WebApplication? _app;

    public Uri BaseAddress { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _app = HttpHost.BuildApplication(port: 0);
        await _app.StartAsync();
        // Listen(IPAddress.Loopback, 0) で起動したあと、実際にバインドされた URL は
        // IServerAddressesFeature 経由 (app.Urls) で取得できる。
        var url = _app.Urls.FirstOrDefault()
            ?? throw new InvalidOperationException("Failed to determine bound URL for the test HTTP server.");
        BaseAddress = new Uri(url);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            try { await _app.StopAsync(); } catch { }
            await _app.DisposeAsync();
            _app = null;
        }
    }
}

/// <summary>
/// Adact HTTP サーバーを共有しつつ、UIA 操作を伴うテストとの並列実行を抑止するための collection。
/// L4 Smoke / L5 E2E をまとめて直列に実行する。
/// </summary>
[CollectionDefinition("AdactHttp", DisableParallelization = true)]
public sealed class AdactHttpCollection : ICollectionFixture<AdactHttpServerFixture>
{
}
