using System.Text.Json;

using Adact.Cli.Commands;
using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Command Helpers behavior.</summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class CommandHelpersTests
{
    private sealed class FakeClient : IAdactMcpClient
    {
        private readonly Queue<CallToolResult> _results = new();

        /// <summary>Gets the Calls value.</summary>
        public List<(string Name, IReadOnlyDictionary<string, object?>? Arguments)> Calls { get; } = [];

        /// <summary>Performs the Enqueue operation.</summary>
        public void Enqueue(CallToolResult result) => _results.Enqueue(result);

        /// <summary>Releases resources.</summary>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        /// <summary>Performs the Call Tool Async operation.</summary>
        public ValueTask<CallToolResult> CallToolAsync(
            string name,
            IReadOnlyDictionary<string, object?>? arguments,
            CancellationToken cancellationToken)
        {
            Calls.Add((name, arguments));
            return ValueTask.FromResult(_results.Dequeue());
        }
    }

    /// <summary>Performs the Write Snapshot Result Async Success Writes Session And Snapshot Path operation.</summary>
    [Fact]
    public async Task WriteSnapshotResultAsync_Success_WritesSessionAndSnapshotPath()
    {
        var client = new FakeClient();
        client.Enqueue(SnapshotResult("s3"));
        var dir = CreateTempDir();

        try
        {
            var (stdout, stderr, exit) = await CaptureAsync(() =>
                CommandHelpers.WriteSnapshotResultAsync(client, sessionId: null, dir, CancellationToken.None));

            Assert.Equal(ExitCodes.Success, exit);
            Assert.Equal(string.Empty, stderr);
            Assert.Contains("result: true", stdout);
            Assert.Contains("sessionId: s3", stdout);
            var snapshotLine = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Single(l => l.StartsWith("snapshotPath:", StringComparison.Ordinal));
            var snapshotPath = snapshotLine["snapshotPath: ".Length..].Split(" (")[0].Trim('"');
            Assert.True(File.Exists(Path.GetFullPath(snapshotPath)), snapshotPath);
            Assert.Contains("filter: operable", File.ReadAllText(Path.GetFullPath(snapshotPath)));

            var call = Assert.Single(client.Calls);
            Assert.Equal("adact_snapshot", call.Name);
            Assert.Null(call.Arguments);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Performs the Write Snapshot Result Async Unknown Filter Returns User Error Without Calling Snapshot operation.</summary>
    [Fact]
    public async Task WriteSnapshotResultAsync_UnknownFilter_ReturnsUserErrorWithoutCallingSnapshot()
    {
        var client = new FakeClient();

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.WriteSnapshotResultAsync(
                client,
                sessionId: "s1",
                snapshotDir: null,
                CancellationToken.None,
                filter: "compact"));

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: INVALID_ARGUMENT", stdout);
        Assert.Empty(client.Calls);
    }

    /// <summary>Performs the Write Snapshot Result Async Snapshot Error Propagates Mcp Error operation.</summary>
    [Fact]
    public async Task WriteSnapshotResultAsync_SnapshotError_PropagatesMcpError()
    {
        var client = new FakeClient();
        client.Enqueue(ErrorResult("SNAPSHOT_FAILED", "snapshot failed"));

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.WriteSnapshotResultAsync(client, "s1", snapshotDir: null, CancellationToken.None));

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: SNAPSHOT_FAILED", stdout);
        Assert.Contains("message: snapshot failed", stdout);
        Assert.Equal("adact_snapshot", Assert.Single(client.Calls).Name);
    }

    /// <summary>Performs the Write Snapshot Result Async Missing Session Id Returns Internal Error operation.</summary>
    [Fact]
    public async Task WriteSnapshotResultAsync_MissingSessionId_ReturnsInternalError()
    {
        var client = new FakeClient();
        client.Enqueue(SnapshotResult(sessionId: null));

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.WriteSnapshotResultAsync(client, sessionId: null, snapshotDir: null, CancellationToken.None));

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: INTERNAL_ERROR", stdout);
        Assert.Contains("adact_snapshot response missing sessionId", stdout);
    }

    /// <summary>Performs the Write Snapshot Result Async Invalid Snapshot Text Returns Internal Error operation.</summary>
    [Fact]
    public async Task WriteSnapshotResultAsync_InvalidSnapshotText_ReturnsInternalError()
    {
        var client = new FakeClient();
        var structured = JsonSerializer.SerializeToElement(new
        {
            _meta = new { sessionId = "s1" },
            tree = new { role = "Window", isEnabled = true, isOffscreen = false },
        });
        client.Enqueue(new CallToolResult
        {
            Content = [new TextContentBlock { Text = "{ invalid json" }],
            StructuredContent = structured,
        });

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.WriteSnapshotResultAsync(client, "s1", snapshotDir: null, CancellationToken.None));

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: INTERNAL_ERROR", stdout);
        Assert.Contains("Failed to parse snapshot response", stdout);
    }

    /// <summary>Performs the Write Snapshot Result Async Raw Filter Writes Raw Filter Frontmatter operation.</summary>
    [Fact]
    public async Task WriteSnapshotResultAsync_RawFilter_WritesRawFilterFrontmatter()
    {
        var client = new FakeClient();
        client.Enqueue(SnapshotResult("s1"));
        var dir = CreateTempDir();

        try
        {
            var (stdout, stderr, exit) = await CaptureAsync(() =>
                CommandHelpers.WriteSnapshotResultAsync(
                    client,
                    "s1",
                    dir,
                    CancellationToken.None,
                    filter: "raw"));

            Assert.Equal(ExitCodes.Success, exit);
            Assert.Equal(string.Empty, stderr);
            var snapshotLine = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Single(l => l.StartsWith("snapshotPath:", StringComparison.Ordinal));
            var text = File.ReadAllText(Path.GetFullPath(snapshotLine["snapshotPath: ".Length..].Split(" (")[0].Trim('"')));
            Assert.Contains("filter: raw", text);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Performs the Run Ref Operation And Auto Snapshot Async No Snapshot Writes Minimal Output operation.</summary>
    [Fact]
    public async Task RunRefOperationAndAutoSnapshotAsync_NoSnapshot_WritesMinimalOutput()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await CaptureAsync(() =>
                CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                    client,
                    "click",
                    "adact_click",
                    new Dictionary<string, object?> { ["ref"] = "s7e9" },
                    "s7e9",
                    true,
                    null,
                    CancellationToken.None));

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("target:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = Assert.Single(client.Calls);
        Assert.Equal("adact_click", call.Name);
        Assert.Equal("s7e9", call.Arguments!["ref"]);
    }

    /// <summary>Performs the Run Ref Operation And Auto Snapshot Async Operation Error Skips Snapshot operation.</summary>
    [Fact]
    public async Task RunRefOperationAndAutoSnapshotAsync_OperationError_SkipsSnapshot()
    {
        var client = new FakeClient();
        client.Enqueue(ErrorResult("ELEMENT_INTERACTION_FAILED", "click failed"));

        var (stdout, stderr, exit) = await CaptureAsync(() =>
                CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                    client,
                    "click",
                    "adact_click",
                    new Dictionary<string, object?> { ["ref"] = "s1e2" },
                    "s1e2",
                    false,
                    null,
                    CancellationToken.None));

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: ELEMENT_INTERACTION_FAILED", stdout);
        Assert.Equal("adact_click", Assert.Single(client.Calls).Name);
    }

    /// <summary>Performs the Run Session Operation And Auto Snapshot Async Success Takes Snapshot For Session operation.</summary>
    [Fact]
    public async Task RunSessionOperationAndAutoSnapshotAsync_Success_TakesSnapshotForSession()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());
        client.Enqueue(SnapshotResult("s4"));
        var dir = CreateTempDir();

        try
        {
            var (stdout, stderr, exit) = await CaptureAsync(() =>
                CommandHelpers.RunSessionOperationAndAutoSnapshotAsync(
                    client,
                    "resize-window",
                    "adact_resize_window",
                    new Dictionary<string, object?> { ["sessionId"] = "s4", ["width"] = 800, ["height"] = 600 },
                    "s4",
                    false,
                    dir,
                    CancellationToken.None));

            Assert.Equal(ExitCodes.Success, exit);
            Assert.Equal(string.Empty, stderr);
            Assert.Contains("snapshotPath:", stdout);
            Assert.DoesNotContain("sessionId:", stdout);
            Assert.DoesNotContain("action:", stdout);
            Assert.Equal(["adact_resize_window", "adact_snapshot"], client.Calls.Select(c => c.Name));
            Assert.Equal("s4", client.Calls[1].Arguments!["sessionId"]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Performs the Run Session Operation And Auto Snapshot Async Operation Error Skips Snapshot operation.</summary>
    [Fact]
    public async Task RunSessionOperationAndAutoSnapshotAsync_OperationError_SkipsSnapshot()
    {
        var client = new FakeClient();
        client.Enqueue(ErrorResult("WINDOW_NOT_FOUND", "window missing"));

        var (stdout, stderr, exit) = await CaptureAsync(() =>
                CommandHelpers.RunSessionOperationAndAutoSnapshotAsync(
                    client,
                    "resize-window",
                    "adact_resize_window",
                    new Dictionary<string, object?> { ["sessionId"] = "s2" },
                    "s2",
                    false,
                    null,
                    CancellationToken.None));

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: WINDOW_NOT_FOUND", stdout);
        Assert.Equal("adact_resize_window", Assert.Single(client.Calls).Name);
    }

    /// <summary>Performs the Run Session Operation And Auto Snapshot Async No Snapshot Writes Minimal Output operation.</summary>
    [Fact]
    public async Task RunSessionOperationAndAutoSnapshotAsync_NoSnapshot_WritesMinimalOutput()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await CaptureAsync(() =>
                CommandHelpers.RunSessionOperationAndAutoSnapshotAsync(
                    client,
                    "maximize",
                    "adact_maximize_window",
                    new Dictionary<string, object?> { ["sessionId"] = "s5" },
                    "s5",
                    true,
                    null,
                    CancellationToken.None));

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("sessionId:", stdout);
        Assert.Equal(string.Empty, stderr);
        Assert.Equal("adact_maximize_window", Assert.Single(client.Calls).Name);
    }

    /// <summary>Performs the Server Option Has Expected Properties operation.</summary>
    [Fact]
    public void ServerOption_HasExpectedProperties()
    {
        Assert.Equal("--server", CommandHelpers.ServerOption.Name);
        Assert.Contains("Connection target URL", CommandHelpers.ServerOption.Description ?? "");
        Assert.True(CommandHelpers.ServerOption.Recursive);
    }

    /// <summary>Performs the Run With Client Async Invalid Server Throws Invalid Url Exception Without Connecting operation.</summary>
    [Fact]
    public async Task RunWithClientAsync_InvalidServer_ThrowsInvalidUrlExceptionWithoutConnecting()
    {
        var connected = false;
        using var _ = CommandHelpers.PushRuntime(new CommandHelpers.CommandRuntime(
            ConnectHttpClientAsync: (_, _) =>
            {
                connected = true;
                return Task.FromResult<IAdactMcpClient>(new FakeClient());
            },
            ConnectNamedPipeClientAsync: DefaultNamedPipeConnectAsync,
            IsServerRunningAsync: NamedPipeMcpClient.IsServerRunningAsync,
            TryAutoStartServerAsync: null));

        await Assert.ThrowsAsync<InvalidUrlException>(() =>
            CommandHelpers.RunWithClientAsync(
                "ftp://localhost/mcp",
                (_, _) => Task.FromResult(ExitCodes.Success),
                CancellationToken.None));

        Assert.False(connected);
    }

    /// <summary>Performs the Run With Client Async Connection Failure Returns Connection Failed operation.</summary>
    [Fact]
    public async Task RunWithClientAsync_ConnectionFailure_ReturnsConnectionFailed()
    {
        using var _ = CommandHelpers.PushRuntime(new CommandHelpers.CommandRuntime(
            ConnectHttpClientAsync: (_, _) => throw new HttpRequestException("daemon unavailable"),
            ConnectNamedPipeClientAsync: DefaultNamedPipeConnectAsync,
            IsServerRunningAsync: NamedPipeMcpClient.IsServerRunningAsync,
            TryAutoStartServerAsync: null));

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.RunWithClientAsync(
                "http://127.0.0.1:41300/mcp",
                (_, _) => Task.FromResult(ExitCodes.Success),
                CancellationToken.None));

        Assert.Equal(ExitCodes.ConnectionFailed, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: CONNECTION_FAILED", stdout);
        Assert.Contains("daemon unavailable", stdout);
    }

    /// <summary>Performs the Run With Client Async Unexpected Connector Exception Returns Command Failed operation.</summary>
    [Fact]
    public async Task RunWithClientAsync_UnexpectedConnectorException_ReturnsCommandFailed()
    {
        using var _ = CommandHelpers.PushRuntime(new CommandHelpers.CommandRuntime(
            ConnectHttpClientAsync: (_, _) => throw new InvalidOperationException("boom"),
            ConnectNamedPipeClientAsync: DefaultNamedPipeConnectAsync,
            IsServerRunningAsync: NamedPipeMcpClient.IsServerRunningAsync,
            TryAutoStartServerAsync: null));

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.RunWithClientAsync(
                "http://127.0.0.1:41300/mcp",
                (_, _) => Task.FromResult(ExitCodes.Success),
                CancellationToken.None));

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: INTERNAL_ERROR", stdout);
        Assert.Contains("boom", stdout);
    }

    private static CallToolResult SuccessResult() => new()
    {
        Content = [],
        StructuredContent = JsonSerializer.SerializeToElement(new { }),
    };

    private static CallToolResult ErrorResult(string code, string message)
    {
        var structured = JsonSerializer.SerializeToElement(new { code, message });
        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = $"{code}: {message}" }],
            StructuredContent = structured,
        };
    }

    private static CallToolResult SnapshotResult(string? sessionId)
    {
        var meta = sessionId is null
            ? """
              "_meta": {"generatedAt": "2026-01-01T00:00:00Z"}
              """
            : $$"""
                "_meta": {
                  "sessionId": "{{sessionId}}",
                  "processName": "calc",
                  "processId": 123,
                  "generatedAt": "2026-01-01T00:00:00Z"
                }
                """;
        var json = $$"""
        {
          {{meta}},
          "tree": {
            "ref": "{{sessionId ?? "s0"}}e1",
            "role": "Window",
            "name": "Calculator",
            "isEnabled": true,
            "isOffscreen": false,
            "children": [
              {
                "ref": "{{sessionId ?? "s0"}}e2",
                "role": "Button",
                "name": "Seven",
                "isEnabled": true,
                "isOffscreen": false
              }
            ]
          }
        }
        """;
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = json }],
            StructuredContent = JsonSerializer.Deserialize<JsonElement>(json),
        };
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "adact-commandhelpers-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static async Task<IAdactMcpClient> DefaultNamedPipeConnectAsync(NamedPipeEndPoint endpoint, CancellationToken ct)
        => await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false);

    private static async Task<(string stdout, string stderr, int exit)> CaptureAsync(Func<Task<int>> action)
    {
        var origOut = Console.Out;
        var origErr = Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        try
        {
            Console.SetOut(outWriter);
            Console.SetError(errWriter);
            var exit = await action().ConfigureAwait(false);
            return (outWriter.ToString(), errWriter.ToString(), exit);
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }
}
