using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Commands;
using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// CLI コマンドの自動起動フローの Unit テスト。
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class AutoStartTests
{
    private sealed class FakeClient : IAdactMcpClient
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<CallToolResult> CallToolAsync(
            string name,
            IReadOnlyDictionary<string, object?>? arguments,
            CancellationToken cancellationToken)
        {
            if (name == "adact_list_windows")
            {
                var json = JsonSerializer.SerializeToElement(new
                {
                    windows = new[]
                    {
                        new
                        {
                            windowRef = "s1e1",
                            sessionId = "s1",
                            processName = "calc",
                            processId = 123,
                            className = "CalcFrame",
                            windowTitle = "Calculator",
                        }
                    }
                });
                return ValueTask.FromResult(new CallToolResult
                {
                    Content = [new TextContentBlock { Text = JsonSerializer.Serialize(json) }],
                    StructuredContent = json,
                });
            }

            if (name == "adact_launch")
            {
                var json = JsonSerializer.SerializeToElement(new { pid = 456, processName = "notepad" });
                return ValueTask.FromResult(new CallToolResult
                {
                    Content = [new TextContentBlock { Text = JsonSerializer.Serialize(json) }],
                    StructuredContent = json,
                });
            }

            var empty = JsonSerializer.SerializeToElement(new { });
            return ValueTask.FromResult(new CallToolResult
            {
                Content = [],
                StructuredContent = empty,
            });
        }
    }

    /// <summary>
    /// list-windows 実行時にサーバーが未起動の場合、TryAutoStartServerAsync が呼ばれることを確認する。
    /// </summary>
    [Fact]
    public async Task ListApps_ServerNotRunning_AutoStartsServer()
    {
        var autoStartCalled = false;
        using var _ = CommandHelpers.PushRuntime(new CommandHelpers.CommandRuntime(
            ConnectHttpClientAsync: static async (endpoint, ct) => await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false),
            ConnectNamedPipeClientAsync: static (_, _) => Task.FromResult<IAdactMcpClient>(new FakeClient()),
            IsServerRunningAsync: static (_, _, _) => Task.FromResult(false),
            TryAutoStartServerAsync: _ =>
            {
                autoStartCalled = true;
                return Task.FromResult(true);
            }));

        var (_, stderr, exit) = await RunAsync(["list-windows"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.True(autoStartCalled);
        Assert.Equal(string.Empty, stderr);
    }

    /// <summary>
    /// launch 実行時にサーバーが未起動の場合、TryAutoStartServerAsync が呼ばれることを確認する。
    /// </summary>
    [Fact]
    public async Task Launch_ServerNotRunning_AutoStartsServer()
    {
        var autoStartCalled = false;
        using var _ = CommandHelpers.PushRuntime(new CommandHelpers.CommandRuntime(
            ConnectHttpClientAsync: static async (endpoint, ct) => await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false),
            ConnectNamedPipeClientAsync: static (_, _) => Task.FromResult<IAdactMcpClient>(new FakeClient()),
            IsServerRunningAsync: static (_, _, _) => Task.FromResult(false),
            TryAutoStartServerAsync: _ =>
            {
                autoStartCalled = true;
                return Task.FromResult(true);
            }));

        var (_, stderr, exit) = await RunAsync(["launch", "notepad"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.True(autoStartCalled);
        Assert.Equal(string.Empty, stderr);
    }

    /// <summary>
    /// click 実行時にサーバーが未起動の場合、TryAutoStartServerAsync が呼ばれず、
    /// CONNECTION_FAILED が返ることを確認する。
    /// </summary>
    [Fact]
    public async Task Click_ServerNotRunning_DoesNotAutoStart_ReturnsConnectionFailed()
    {
        var autoStartCalled = false;
        using var _ = CommandHelpers.PushRuntime(new CommandHelpers.CommandRuntime(
            ConnectHttpClientAsync: static async (endpoint, ct) => await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false),
            ConnectNamedPipeClientAsync: static (_, _) => throw new TimeoutException("connection failed"),
            IsServerRunningAsync: static (_, _, _) => Task.FromResult(false),
            TryAutoStartServerAsync: _ =>
            {
                autoStartCalled = true;
                return Task.FromResult(true);
            }));

        var (stdout, stderr, exit) = await RunAsync(["click", "s1e1", "--no-snapshot"]);

        Assert.Equal(ExitCodes.ConnectionFailed, exit);
        Assert.False(autoStartCalled);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: CONNECTION_FAILED", stdout);
    }

    [Fact]
    public async Task ListApps_AfterAutoStart_RetriesNamedPipeReconnect()
    {
        var connectAttempts = 0;
        using var _ = CommandHelpers.PushRuntime(new CommandHelpers.CommandRuntime(
            ConnectHttpClientAsync: static async (endpoint, ct) => await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false),
            ConnectNamedPipeClientAsync: (_, _) =>
            {
                connectAttempts++;
                if (connectAttempts < 3)
                {
                    throw new TimeoutException("warming up");
                }

                return Task.FromResult<IAdactMcpClient>(new FakeClient());
            },
            IsServerRunningAsync: static (_, _, _) => Task.FromResult(false),
            TryAutoStartServerAsync: static _ => Task.FromResult(true)));

        var (_, stderr, exit) = await RunAsync(["list-windows"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Equal(3, connectAttempts);
    }

    private static async Task<(string stdout, string stderr, int exit)> RunAsync(string[] args)
    {
        var origOut = Console.Out;
        var origErr = Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        try
        {
            Console.SetOut(outWriter);
            Console.SetError(errWriter);

            var root = Program.BuildRoot();
            var parse = root.Parse(args);
            var exit = await parse.InvokeAsync().ConfigureAwait(false);
            return (outWriter.ToString(), errWriter.ToString(), exit);
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }
}
