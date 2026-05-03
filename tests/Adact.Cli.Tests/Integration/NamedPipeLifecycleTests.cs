using System.IO.Pipes;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Server.NamedPipe;
using Adact.Tests.Common;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Integration;

/// <summary>
/// Named Pipe MCP サーバーの起動・停止ライフサイクルを検証する統合テスト。
/// 対話デスクトップセッションが必要。
/// </summary>
public sealed class NamedPipeLifecycleTests
{
    private static NamedPipeEndPoint CreateUniqueEndpoint()
    {
        var hash = Guid.NewGuid().ToString("N")[..16];
        var pipeName = $@"{NamedPipeEndPoint.PipePrefix}{NamedPipeEndPoint.AdactPipePrefix}{hash}-test";
        return NamedPipeEndPoint.Parse(pipeName);
    }

    private static async Task WaitForServerAsync(NamedPipeEndPoint endpoint, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (await NamedPipeMcpClient.IsServerRunningAsync(endpoint, timeoutMs: 100, ct).ConfigureAwait(false))
                return;
            await Task.Delay(100, ct).ConfigureAwait(false);
        }
        throw new OperationCanceledException("Server did not become ready within the allotted time.");
    }

    /// <summary>
    /// NamedPipeHost を起動し、NamedPipeMcpClient で接続して daemon_stop を呼び出すと
    /// サーバーが停止し、2回目の接続で失敗することを確認する。
    /// </summary>
    [Trait("Layer", "Integration")]
    [InteractiveFact]
    public async Task NamedPipeServer_StartAndStop_Success()
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var endpoint = CreateUniqueEndpoint();

        using var serverCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serverCts.Token, timeoutCts.Token);

        var serverTask = Task.Run(async () =>
        {
            try
            {
                return await NamedPipeHost.RunAsync(endpoint.PipeName, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (serverCts.IsCancellationRequested || timeoutCts.IsCancellationRequested)
            {
                return 0;
            }
        }, timeoutCts.Token);

        try
        {
            await WaitForServerAsync(endpoint, timeoutCts.Token).ConfigureAwait(false);

            await using var client = await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, timeoutCts.Token).ConfigureAwait(false);

            try
            {
                var result = await client.CallToolAsync("daemon_stop", arguments: null, timeoutCts.Token).ConfigureAwait(false);

                Assert.False(result.IsError ?? false);
                var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
                Assert.Contains("\"stopped\":true", text, StringComparison.Ordinal);
            }
            catch (IOException)
            {
                // daemon_stop の応答前にサーバーがシャットダウンして接続が切断されるケースは正常
            }

            // サーバーが停止するまで待機
            await Task.Delay(500, timeoutCts.Token).ConfigureAwait(false);
            Assert.False(await NamedPipeMcpClient.IsServerRunningAsync(endpoint, 1000, timeoutCts.Token).ConfigureAwait(false));

            // 2回目の接続は失敗する
            var ex = await Record.ExceptionAsync(async () =>
                await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, timeoutCts.Token).ConfigureAwait(false));

            Assert.NotNull(ex);
            Assert.True(
                ex is TimeoutException or IOException,
                $"Expected connection failure after stop, got {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            serverCts.Cancel();
            try { await serverTask.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); } catch { }
        }
    }

    /// <summary>
    /// サーバーを起動して daemon_stop した後、再度接続を試みると失敗することを確認する。
    /// </summary>
    [Trait("Layer", "Integration")]
    [InteractiveFact]
    public async Task NamedPipeServer_DoubleStop_SecondReturnsNoDaemon()
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var endpoint = CreateUniqueEndpoint();

        using var serverCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serverCts.Token, timeoutCts.Token);

        var serverTask = Task.Run(async () =>
        {
            try
            {
                return await NamedPipeHost.RunAsync(endpoint.PipeName, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (serverCts.IsCancellationRequested || timeoutCts.IsCancellationRequested)
            {
                return 0;
            }
        }, timeoutCts.Token);

        try
        {
            await WaitForServerAsync(endpoint, timeoutCts.Token).ConfigureAwait(false);

            // 1回目: 接続して daemon_stop を呼び出す
            await using var client = await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, timeoutCts.Token).ConfigureAwait(false);
            try
            {
                var result = await client.CallToolAsync("daemon_stop", arguments: null, timeoutCts.Token).ConfigureAwait(false);
                Assert.False(result.IsError ?? false);
            }
            catch (IOException)
            {
                // daemon_stop の応答前にサーバーがシャットダウンして接続が切断されるケースは正常
            }

            // サーバー停止を待機
            await Task.Delay(500, timeoutCts.Token).ConfigureAwait(false);
            Assert.False(await NamedPipeMcpClient.IsServerRunningAsync(endpoint, 1000, timeoutCts.Token).ConfigureAwait(false));

            // 2回目: サーバーが停止しているので接続できない → "No daemon is running" 相当
            var ex = await Record.ExceptionAsync(async () =>
                await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, timeoutCts.Token).ConfigureAwait(false));

            Assert.NotNull(ex);
            Assert.True(
                ex is TimeoutException or IOException,
                $"Expected 'No daemon is running' behavior (connection failure), got {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            serverCts.Cancel();
            try { await serverTask.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); } catch { }
        }
    }

    [Trait("Layer", "E2E")]
    [InteractiveFact]
    public async Task NamedPipeServer_ListAppsAndAttachAcrossConnections_SharesDaemonState()
    {
        InteractiveTestGuard.SkipIfNotInteractive();
        using var _calcLock = new CalculatorMutex();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var endpoint = CreateUniqueEndpoint();
        var calculator = await CalculatorTestHelper.StartFreshCalculatorAsync(TimeSpan.FromSeconds(10));

        using var serverCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serverCts.Token, timeoutCts.Token);

        var serverTask = Task.Run(async () =>
        {
            try
            {
                return await NamedPipeHost.RunAsync(endpoint.PipeName, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (serverCts.IsCancellationRequested || timeoutCts.IsCancellationRequested)
            {
                return 0;
            }
        }, timeoutCts.Token);

        try
        {
            await WaitForServerAsync(endpoint, timeoutCts.Token).ConfigureAwait(false);

            string windowRef;
            await using (var client1 = await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, timeoutCts.Token).ConfigureAwait(false))
            {
                var list = await client1.CallToolAsync("windows_list_apps", arguments: null, timeoutCts.Token).ConfigureAwait(false);
                Assert.False(list.IsError ?? false);
                windowRef = FindCalculatorWindowRef(list);
            }

            await using var client2 = await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, timeoutCts.Token).ConfigureAwait(false);
            var attach = await client2.CallToolAsync(
                "windows_attach",
                new Dictionary<string, object?> { ["windowRef"] = windowRef },
                timeoutCts.Token).ConfigureAwait(false);

            Assert.False(attach.IsError ?? false,
                $"windows_attach failed: {(attach.Content.FirstOrDefault() as TextContentBlock)?.Text}");
            var payload = attach.StructuredContent!.Value;
            Assert.Equal(windowRef, payload.GetProperty("windowRef").GetString());
            Assert.StartsWith("s", payload.GetProperty("sessionId").GetString(), StringComparison.Ordinal);
        }
        finally
        {
            serverCts.Cancel();
            CalculatorTestHelper.KillCalculatorProcesses();
            try { calculator?.Dispose(); } catch { }
            try { await serverTask.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); } catch { }
        }
    }

    private static string FindCalculatorWindowRef(CallToolResult listResult)
    {
        var text = (listResult.Content.FirstOrDefault() as TextContentBlock)?.Text;
        Assert.NotNull(text);

        using var doc = JsonDocument.Parse(text!);
        foreach (var window in doc.RootElement.EnumerateArray())
        {
            var processName = window.TryGetProperty("processName", out var processNameNode)
                ? processNameNode.GetString()
                : null;
            var windowTitle = window.TryGetProperty("windowTitle", out var titleNode)
                ? titleNode.GetString()
                : null;

            if ((processName?.Contains("Calculator", StringComparison.OrdinalIgnoreCase) ?? false)
                || (windowTitle?.Contains("Calculator", StringComparison.OrdinalIgnoreCase) ?? false)
                || (windowTitle?.Contains("電卓", StringComparison.Ordinal) ?? false))
            {
                return window.GetProperty("windowRef").GetString()
                    ?? throw new Xunit.Sdk.XunitException("Calculator entry was missing windowRef.");
            }
        }

        throw new Xunit.Sdk.XunitException($"Calculator windowRef not found in windows_list_apps output: {text}");
    }
}
