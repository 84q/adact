using System.Diagnostics;
using System.Text.Json;

using Adact.Mcp.Http.Tests;
using Adact.Tests.Common;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Http.Tests.E2E;

/// <summary>Contains tests for the Sample App Http E2 E behavior.</summary>
[Trait("Layer", "E2E")]
[Collection("AdactHttp")]
public class SampleAppHttpE2ETests
{
    private readonly AdactHttpServerFixture _fixture;

    /// <summary>Initializes a new instance of the Sample App Http E2 ETests class.</summary>
    public SampleAppHttpE2ETests(AdactHttpServerFixture fixture)
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

    /// <summary>Performs the Attach And Snapshot On Sample App Returns Tree With Buttons operation.</summary>
    [InteractiveFact]
    public async Task AttachAndSnapshot_OnSampleApp_ReturnsTreeWithButtons()
    {
        InteractiveTestGuard.SkipIfNotInteractive();

        using var _appLock = new SampleAppMutex();
        var sampleApp = _fixture.UsesExternalServer
            ? null
            : await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await using var client = await McpClient.CreateAsync(CreateTransport(), cancellationToken: cts.Token);

            if (_fixture.UsesExternalServer)
            {
                var launch = await client.CallToolAsync(
                    "adact_launch",
                    new Dictionary<string, object?> { ["executable"] = "SampleApp.exe" },
                    cancellationToken: cts.Token);
                Assert.False(launch.IsError ?? false,
                    $"adact_launch failed: {(launch.Content.FirstOrDefault() as TextContentBlock)?.Text}");
            }

            string? windowRef = null;
            string? listText = null;
            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
            while (DateTimeOffset.UtcNow < deadline)
            {
                var listResult = await client.CallToolAsync("adact_list_windows", cancellationToken: cts.Token);
                Assert.False(listResult.IsError ?? false,
                    $"adact_list_windows failed: {(listResult.Content.FirstOrDefault() as TextContentBlock)?.Text}");
                listText = (listResult.Content[0] as TextContentBlock)?.Text;
                Assert.NotNull(listText);

                windowRef = SampleAppWindowFinder.FindWindowRef(listText!);
                if (!string.IsNullOrEmpty(windowRef))
                {
                    break;
                }

                await Task.Delay(200, cts.Token);
            }
            Assert.False(string.IsNullOrEmpty(windowRef),
                $"SampleApp windowRef not found in adact_list_windows output: {listText}");

            var attach = await client.CallToolAsync(
                "adact_attach",
                new Dictionary<string, object?> { ["windowRef"] = windowRef! },
                cancellationToken: cts.Token);
            Assert.False(attach.IsError ?? false,
                $"adact_attach failed: {(attach.Content.FirstOrDefault() as TextContentBlock)?.Text}");

            // snapshot (active session)
            var snap = await client.CallToolAsync("adact_snapshot", cancellationToken: cts.Token);
            Assert.False(snap.IsError ?? false,
                $"adact_snapshot failed: {(snap.Content.FirstOrDefault() as TextContentBlock)?.Text}");
            var snapText = (snap.Content[0] as TextContentBlock)?.Text;
            Assert.NotNull(snapText);

            using var doc = JsonDocument.Parse(snapText!);
            var tree = doc.RootElement.GetProperty("tree");
            var buttonCount = CountByRole(tree, "Button");
            Assert.True(buttonCount > 1,
                $"SampleApp snapshot should contain multiple Button nodes; got {buttonCount}.");

            if (_fixture.UsesExternalServer)
            {
                await client.CallToolAsync("adact_close_window", cancellationToken: cts.Token);
            }
        }
        finally
        {
            if (!_fixture.UsesExternalServer)
            {
                SampleAppTestHelper.KillSampleAppProcesses();
            }
            try { sampleApp?.Dispose(); } catch { }
        }
    }

    private static int CountByRole(JsonElement node, string role)
    {
        int count = 0;
        if (node.TryGetProperty("role", out var r) && r.ValueKind == JsonValueKind.String && r.GetString() == role)
            count++;
        if (node.TryGetProperty("children", out var children))
            foreach (var ch in children.EnumerateArray())
                count += CountByRole(ch, role);
        return count;
    }
}
