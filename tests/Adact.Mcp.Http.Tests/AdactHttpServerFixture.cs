using System.Net;

using Adact.Cli.Server;

using Microsoft.AspNetCore.Builder;

using Xunit;

namespace Adact.Mcp.Http.Tests;

/// <summary>
/// HttpHost.BuildApplication(port:0) で実 Kestrel を起動し、ephemeral port を採番させる
/// xUnit フィクスチャ。テスト間でサーバーを再利用する (Stateless モードのため共有可能)。
/// </summary>
public sealed class AdactHttpServerFixture : IAsyncLifetime
{
    internal const string ServerUrlEnvironmentVariable = "ADACT_SERVER_URL";

    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);

    private WebApplication? _app;
    private bool _usesExternalServer;

    /// <summary>
    /// テスト中に MCP クライアントから接続する HTTP エンドポイント (/mcp 付き)。
    /// </summary>
    public Uri BaseAddress { get; private set; } = null!;

    /// <summary>True when tests are connected to an externally started daemon from <c>ADACT_SERVER_URL</c>.</summary>
    public bool UsesExternalServer => _usesExternalServer;

    /// <summary>
    /// HTTP サーバーを起動して <see cref="BaseAddress"/> を解決する。
    /// </summary>
    /// <returns>初期化完了タスク。</returns>
    public async Task InitializeAsync()
    {
        var externalBaseAddress = GetExternalServerUri();
        if (externalBaseAddress is not null)
        {
            BaseAddress = externalBaseAddress;
            _usesExternalServer = true;
            await WaitForReadyAsync(BaseAddress, StartupTimeout).ConfigureAwait(false);
            return;
        }

        _app = HttpHost.BuildApplication(IPAddress.Loopback, 0);
        await _app.StartAsync();
        // Listen(IPAddress.Loopback, 0) で起動したあと、実際にバインドされた URL は
        // IServerAddressesFeature 経由 (app.Urls) で取得できる。
        var url = _app.Urls.FirstOrDefault()
            ?? throw new InvalidOperationException("Failed to determine bound URL for the test HTTP server.");
        // Phase 5: MCP は /mcp にマップされている (009 §2.2)。クライアントの Endpoint も /mcp 付きにする。
        BaseAddress = new Uri(new Uri(url), HttpHost.McpPath);
    }

    /// <summary>
    /// 起動中の HTTP サーバーを停止し、リソースを解放する。
    /// </summary>
    /// <returns>解放完了タスク。</returns>
    public async Task DisposeAsync()
    {
        if (_usesExternalServer) return;

        if (_app is not null)
        {
            try { await _app.StopAsync(); } catch { }
            await _app.DisposeAsync();
            _app = null;
        }
    }

    internal static Uri? GetExternalServerUri()
    {
        var value = Environment.GetEnvironmentVariable(ServerUrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value)) return null;

        var url = value.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"{ServerUrlEnvironmentVariable} must be an absolute http(s) URL, e.g. http://127.0.0.1:41300/mcp.");
        }

        return uri;
    }

    private static async Task WaitForReadyAsync(Uri baseAddress, TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Exception? last = null;
        while (sw.Elapsed < timeout)
        {
            try
            {
                using var _ = await http.GetAsync(baseAddress).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(200).ConfigureAwait(false);
            }
        }

        throw new TimeoutException(
            $"External adact HTTP daemon did not become ready within {timeout.TotalSeconds:F0}s on {baseAddress}.",
            last);
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
