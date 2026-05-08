using System.IO.Pipes;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Server.NamedPipe;
using Adact.Tests.Common;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Integration;

/// <summary>Contains tests for the Named Pipe Lifecycle behavior.</summary>
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

    /// <summary>Performs the Named Pipe Server Start And Stop Success operation.</summary>
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
                var result = await client.CallToolAsync("adact_daemon_stop", arguments: null, timeoutCts.Token).ConfigureAwait(false);

                Assert.False(result.IsError ?? false);
                var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
                Assert.Contains("\"stopped\":true", text, StringComparison.Ordinal);
            }
            catch (IOException)
            {
            }

            await Task.Delay(500, timeoutCts.Token).ConfigureAwait(false);
            Assert.False(await NamedPipeMcpClient.IsServerRunningAsync(endpoint, 1000, timeoutCts.Token).ConfigureAwait(false));

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

    /// <summary>Performs the Named Pipe Server Double Stop Second Returns No Daemon operation.</summary>
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

            await using var client = await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, timeoutCts.Token).ConfigureAwait(false);
            try
            {
                var result = await client.CallToolAsync("adact_daemon_stop", arguments: null, timeoutCts.Token).ConfigureAwait(false);
                Assert.False(result.IsError ?? false);
            }
            catch (IOException)
            {
            }

            await Task.Delay(500, timeoutCts.Token).ConfigureAwait(false);
            Assert.False(await NamedPipeMcpClient.IsServerRunningAsync(endpoint, 1000, timeoutCts.Token).ConfigureAwait(false));

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

    /// <summary>Performs the Named Pipe Server List Apps And Attach Across Connections Shares Daemon State operation.</summary>
    [Trait("Layer", "E2E")]
    [InteractiveFact]
    public async Task NamedPipeServer_ListAppsAndAttachAcrossConnections_SharesDaemonState()
    {
        InteractiveTestGuard.SkipIfNotInteractive();
        using var _appLock = new SampleAppMutex();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var endpoint = CreateUniqueEndpoint();
        var sampleApp = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));

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
                var list = await client1.CallToolAsync("adact_list_windows", arguments: null, timeoutCts.Token).ConfigureAwait(false);
                Assert.False(list.IsError ?? false);
                windowRef = FindSampleAppWindowRef(list);
            }

            await using var client2 = await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, timeoutCts.Token).ConfigureAwait(false);
            var attach = await client2.CallToolAsync(
                "adact_attach",
                new Dictionary<string, object?> { ["windowRef"] = windowRef },
                timeoutCts.Token).ConfigureAwait(false);

            Assert.False(attach.IsError ?? false,
                $"adact_attach failed: {(attach.Content.FirstOrDefault() as TextContentBlock)?.Text}");
            var payload = attach.StructuredContent!.Value;
            Assert.Equal(windowRef, payload.GetProperty("windowRef").GetString());
            Assert.StartsWith("s", payload.GetProperty("sessionId").GetString(), StringComparison.Ordinal);
        }
        finally
        {
            serverCts.Cancel();
            SampleAppTestHelper.KillSampleAppProcesses();
            try { sampleApp?.Dispose(); } catch { }
            try { await serverTask.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); } catch { }
        }
    }

    private static string FindSampleAppWindowRef(CallToolResult listResult)
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

            if ((processName?.Contains("SampleApp", StringComparison.OrdinalIgnoreCase) ?? false)
                || (windowTitle?.Contains("ADACT SampleApp", StringComparison.Ordinal) ?? false))
            {
                return window.GetProperty("windowRef").GetString()
                    ?? throw new Xunit.Sdk.XunitException("SampleApp entry was missing windowRef.");
            }
        }

        throw new Xunit.Sdk.XunitException($"SampleApp windowRef not found in adact_list_windows output: {text}");
    }
}
