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
            if (name == "windows_list_apps")
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

            if (name == "windows_launch")
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
    /// list-apps 実行時にサーバーが未起動の場合、TryAutoStartServerAsync が呼ばれることを確認する。
    /// </summary>
    [Fact]
    public async Task ListApps_ServerNotRunning_AutoStartsServer()
    {
        var origIsRunning = CommandHelpers.IsServerRunningAsync;
        var origConnect = CommandHelpers.ConnectNamedPipeClientAsync;
        var origAutoStart = CommandHelpers.TryAutoStartServerAsync;
        try
        {
            var autoStartCalled = false;
            CommandHelpers.IsServerRunningAsync = static (_, _, _) => Task.FromResult(false);
            CommandHelpers.ConnectNamedPipeClientAsync = static (_, _) => Task.FromResult<IAdactMcpClient>(new FakeClient());
            CommandHelpers.TryAutoStartServerAsync = _ =>
            {
                autoStartCalled = true;
                return Task.FromResult(true);
            };

            var (_, stderr, exit) = await RunAsync(["list-apps"]);

            Assert.Equal(ExitCodes.Success, exit);
            Assert.True(autoStartCalled);
            Assert.Equal(string.Empty, stderr);
        }
        finally
        {
            CommandHelpers.IsServerRunningAsync = origIsRunning;
            CommandHelpers.ConnectNamedPipeClientAsync = origConnect;
            CommandHelpers.TryAutoStartServerAsync = origAutoStart;
        }
    }

    /// <summary>
    /// launch 実行時にサーバーが未起動の場合、TryAutoStartServerAsync が呼ばれることを確認する。
    /// </summary>
    [Fact]
    public async Task Launch_ServerNotRunning_AutoStartsServer()
    {
        var origIsRunning = CommandHelpers.IsServerRunningAsync;
        var origConnect = CommandHelpers.ConnectNamedPipeClientAsync;
        var origAutoStart = CommandHelpers.TryAutoStartServerAsync;
        try
        {
            var autoStartCalled = false;
            CommandHelpers.IsServerRunningAsync = static (_, _, _) => Task.FromResult(false);
            CommandHelpers.ConnectNamedPipeClientAsync = static (_, _) => Task.FromResult<IAdactMcpClient>(new FakeClient());
            CommandHelpers.TryAutoStartServerAsync = _ =>
            {
                autoStartCalled = true;
                return Task.FromResult(true);
            };

            var (_, stderr, exit) = await RunAsync(["launch", "notepad"]);

            Assert.Equal(ExitCodes.Success, exit);
            Assert.True(autoStartCalled);
            Assert.Equal(string.Empty, stderr);
        }
        finally
        {
            CommandHelpers.IsServerRunningAsync = origIsRunning;
            CommandHelpers.ConnectNamedPipeClientAsync = origConnect;
            CommandHelpers.TryAutoStartServerAsync = origAutoStart;
        }
    }

    /// <summary>
    /// click 実行時にサーバーが未起動の場合、TryAutoStartServerAsync が呼ばれず、
    /// CONNECTION_FAILED が返ることを確認する。
    /// </summary>
    [Fact]
    public async Task Click_ServerNotRunning_DoesNotAutoStart_ReturnsConnectionFailed()
    {
        var origIsRunning = CommandHelpers.IsServerRunningAsync;
        var origConnect = CommandHelpers.ConnectNamedPipeClientAsync;
        var origAutoStart = CommandHelpers.TryAutoStartServerAsync;
        try
        {
            var autoStartCalled = false;
            CommandHelpers.IsServerRunningAsync = static (_, _, _) => Task.FromResult(false);
            CommandHelpers.ConnectNamedPipeClientAsync = static (_, _) => throw new TimeoutException("connection failed");
            CommandHelpers.TryAutoStartServerAsync = _ =>
            {
                autoStartCalled = true;
                return Task.FromResult(true);
            };

            var (_, stderr, exit) = await RunAsync(["click", "s1e1", "--no-snapshot"]);

            Assert.Equal(ExitCodes.ConnectionFailed, exit);
            Assert.False(autoStartCalled);
            Assert.Contains("error CONNECTION_FAILED", stderr);
        }
        finally
        {
            CommandHelpers.IsServerRunningAsync = origIsRunning;
            CommandHelpers.ConnectNamedPipeClientAsync = origConnect;
            CommandHelpers.TryAutoStartServerAsync = origAutoStart;
        }
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
