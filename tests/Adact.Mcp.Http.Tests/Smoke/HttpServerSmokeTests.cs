using System.Text.Json;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Http.Tests.Smoke;

[Trait("Layer", "Smoke")]
[Collection("AdactHttp")]
public class HttpServerSmokeTests
{
  private readonly AdactHttpServerFixture _fixture;

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

  [Fact]
  public async Task Initialize_OnRunningHttpServer_ReturnsServerInfo()
  {
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await using var client = await McpClient.CreateAsync(CreateTransport(), cancellationToken: cts.Token);

    Assert.Equal("adact", client.ServerInfo.Name);
  }

  [Fact]
  public async Task ListApps_OnRunningHttpServer_ReturnsNonEmpty()
  {
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
