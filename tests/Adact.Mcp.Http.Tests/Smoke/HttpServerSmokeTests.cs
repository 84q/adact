using System.Text.Json;

using Adact.Tests.Common;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Http.Tests.Smoke;

/// <summary>Contains tests for the Http Server Smoke behavior.</summary>
[Trait("Layer", "Smoke")]
[Collection("AdactHttp")]
public class HttpServerSmokeTests
{
    private readonly AdactHttpServerFixture _fixture;

    /// <summary>Initializes a new instance of the Http Server Smoke Tests class.</summary>
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

    /// <summary>Initializes the fixture.</summary>
    [Fact]
    public async Task Initialize_OnRunningHttpServer_ReturnsServerInfo()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = await McpClient.CreateAsync(CreateTransport(), cancellationToken: cts.Token);

        Assert.Equal("adact", client.ServerInfo.Name);
    }

    /// <summary>Performs the List Apps On Running Http Server Returns Non Empty operation.</summary>
    [InteractiveFact]
    public async Task ListApps_OnRunningHttpServer_ReturnsNonEmpty()
    {
        InteractiveTestGuard.SkipIfNotInteractive();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = await McpClient.CreateAsync(CreateTransport(), cancellationToken: cts.Token);

        var result = await client.CallToolAsync(
            "adact_list_windows",
            cancellationToken: cts.Token);
        Assert.False(result.IsError ?? false);
        var text = (result.Content[0] as TextContentBlock)?.Text;
        Assert.NotNull(text);
        using var doc = JsonDocument.Parse(text!);
        Assert.True(doc.RootElement.GetArrayLength() > 0,
            "adact_list_windows should return at least one window on a running desktop session.");
    }
}
