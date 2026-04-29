using System.Diagnostics;
using System.Text.Json;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Http.Tests.E2E;

/// <summary>
/// HTTP daemon 経由で実 calc.exe に attach し、snapshot まで一連の MCP ツールを E2E で検証するテスト群。
/// HTTP トランスポートと UIA 操作パイプライン全体の回帰を E2E レイヤーで防ぐため。
/// </summary>
[Trait("Layer", "E2E")]
[Collection("AdactHttp")]
public class CalculatorHttpE2ETests
{
    private readonly AdactHttpServerFixture _fixture;

    /// <summary>
    /// 共有 HTTP サーバーフィクスチャを受け取る xUnit コンストラクタ。
    /// </summary>
    /// <param name="fixture">テスト全体で共有される <see cref="AdactHttpServerFixture"/>。</param>
    public CalculatorHttpE2ETests(AdactHttpServerFixture fixture)
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
    /// 電卓 (calc.exe) を起動し HTTP MCP 経由で windows_attach → windows_snapshot を実行し、
    /// snapshot tree に複数の Button ノードが含まれることを確認する。
    /// HTTP トランスポート + UIA + ref 採番の E2E 通しシナリオの回帰防止。
    /// </summary>
    /// <returns>テスト完了タスク。</returns>
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

            // 電卓の windowRef を windows_list_apps から取得する
            var listResult = await client.CallToolAsync("windows_list_apps", cancellationToken: cts.Token);
            Assert.False(listResult.IsError ?? false,
                $"windows_list_apps failed: {(listResult.Content.FirstOrDefault() as TextContentBlock)?.Text}");
            var listText = (listResult.Content[0] as TextContentBlock)?.Text;
            Assert.NotNull(listText);
            string? windowRef = null;
            using (var listDoc = JsonDocument.Parse(listText!))
            {
                foreach (var item in listDoc.RootElement.EnumerateArray())
                {
                    if (item.TryGetProperty("windowTitle", out var t)
                        && t.ValueKind == JsonValueKind.String
                        && string.Equals(t.GetString(), "電卓", StringComparison.Ordinal))
                    {
                        windowRef = item.GetProperty("windowRef").GetString();
                        break;
                    }
                }
            }
            Assert.NotNull(windowRef);

            // attach (windowRef 経由)
            var attach = await client.CallToolAsync(
                "windows_attach",
                new Dictionary<string, object?> { ["windowRef"] = windowRef! },
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
