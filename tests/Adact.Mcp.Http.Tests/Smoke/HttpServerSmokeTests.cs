using System.Text.Json;

using Adact.Tests.Common;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Http.Tests.Smoke;

/// <summary>
/// HTTP daemon が起動して MCP initialize / 軽量ツール呼び出しに応答できるかを確認する Smoke テスト群。
/// E2E より浅い層で HTTP ルーティング・MCP ハンドシェイクの回帰を素早く検出する。
/// </summary>
[Trait("Layer", "Smoke")]
[Collection("AdactHttp")]
public class HttpServerSmokeTests
{
    private readonly AdactHttpServerFixture _fixture;

    /// <summary>
    /// 共有 HTTP サーバーフィクスチャを受け取る xUnit コンストラクタ。
    /// </summary>
    /// <param name="fixture">テスト全体で共有される <see cref="AdactHttpServerFixture"/>。</param>
    public HttpServerSmokeTests(AdactHttpServerFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpClientTransport CreateTransport()
    {
        return new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = _fixture.BaseAddress,
            TransportMode = HttpTransportMode.StreamableHttp,
            Name = "adact-http-test",
        });
    }

    /// <summary>
    /// HTTP MCP の初期化応答に server name "adact" が含まれることを確認する。
    /// MCP 初期化ハンドシェイク全体の回帰を素早く検出する Smoke。
    /// </summary>
    /// <returns>テスト完了タスク。</returns>
    [Fact]
    public async Task Initialize_OnRunningHttpServer_ReturnsServerInfo()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = await McpClient.CreateAsync(CreateTransport(), cancellationToken: cts.Token);

        Assert.Equal("adact", client.ServerInfo.Name);
    }

    /// <summary>
    /// HTTP MCP 経由で windows_list_apps を呼び、生存ウィンドウが 1 件以上返ることを確認する。
    /// HTTP 配線と最も軽量な UIA ツールの疎通を Smoke として検出するため。
    /// </summary>
    /// <returns>テスト完了タスク。</returns>
    [InteractiveFact]
    public async Task ListApps_OnRunningHttpServer_ReturnsNonEmpty()
    {
        InteractiveTestGuard.SkipIfNotInteractive();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = await McpClient.CreateAsync(CreateTransport(), cancellationToken: cts.Token);

        var result = await client.CallToolAsync(
            "windows_list_apps",
            cancellationToken: cts.Token);
        Assert.False(result.IsError ?? false);
        var text = (result.Content[0] as TextContentBlock)?.Text;
        Assert.NotNull(text);
        using var doc = JsonDocument.Parse(text!);
        Assert.True(doc.RootElement.GetArrayLength() > 0,
            "windows_list_apps should return at least one window on a running desktop session.");
    }
}
