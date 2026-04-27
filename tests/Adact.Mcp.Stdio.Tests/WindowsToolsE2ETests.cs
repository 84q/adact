using System.Diagnostics;
using System.Text.Json;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Stdio.Tests;

[Trait("Layer", "E2E")]
[Collection("UiaSerial")]
public class WindowsToolsE2ETests
{
    private static StdioClientTransport CreateTransport()
    {
        return new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "adact",
            Command = AdactExePath.Resolve(),
            Arguments = ["local"],
        });
    }

    [Fact]
    public async Task ListApps_OnRunningSystem_ReturnsNonEmpty()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = await McpClient.CreateAsync(CreateTransport(), cancellationToken: cts.Token);

        var result = await client.CallToolAsync("windows_list_apps", cancellationToken: cts.Token);
        Assert.False(result.IsError ?? false);
        var text = (result.Content[0] as TextContentBlock)?.Text;
        Assert.NotNull(text);
        using var doc = JsonDocument.Parse(text!);
        Assert.True(doc.RootElement.GetArrayLength() > 0,
            "windows_list_apps should return at least one window on a running desktop session.");
    }

    [Fact]
    public async Task AttachAndSnapshot_OnCalculator_ReturnsTreeWithButtons()
    {
        // calc.exe を使う E2E をアセンブリ間並列でも直列化するための named semaphore
        using var _calcLock = new CalculatorMutex();
        var calculator = StartCalculator();
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await using var client = await McpClient.CreateAsync(CreateTransport(), cancellationToken: cts.Token);

            // attach
            var attach = await client.CallToolAsync(
                "windows_attach",
                new Dictionary<string, object?> { ["windowTitle"] = "電卓" },
                cancellationToken: cts.Token);
            Assert.False(attach.IsError ?? false,
                $"windows_attach failed: {(attach.Content.FirstOrDefault() as TextContentBlock)?.Text}");

            // snapshot (active session)
            var snap = await client.CallToolAsync("windows_snapshot", cancellationToken: cts.Token);
            Assert.False(snap.IsError ?? false,
                $"windows_snapshot failed: {(snap.Content.FirstOrDefault() as TextContentBlock)?.Text}");
            var snapText = (snap.Content[0] as TextContentBlock)?.Text;
            Assert.NotNull(snapText);

            using var doc = JsonDocument.Parse(snapText!);
            var tree = doc.RootElement.GetProperty("tree");
            var buttonCount = CountByRole(tree, "Button");
            Assert.True(buttonCount > 1,
                $"Calculator snapshot should contain multiple Button nodes; got {buttonCount}.");
        }
        finally
        {
            foreach (var p in Process.GetProcessesByName("CalculatorApp"))
            {
                try { p.Kill(); p.WaitForExit(2000); } catch { }
            }
            try { calculator?.Dispose(); } catch { }
        }
    }

    private static Process? StartCalculator()
    {
        var p = Process.Start(new ProcessStartInfo { FileName = "calc.exe", UseShellExecute = true });
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (Process.GetProcessesByName("CalculatorApp").Length > 0)
            {
                Thread.Sleep(1000);
                return p;
            }
            Thread.Sleep(150);
        }
        return p;
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
