using System.Text.Json;

using Adact.Cli.Commands;
using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// Unit tests for snapshot output and auto-snapshot flows in <see cref="CommandHelpers"/>.
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class CommandHelpersTests
{
    private sealed class FakeClient : IAdactMcpClient
    {
        private readonly Queue<CallToolResult> _results = new();

        public List<(string Name, IReadOnlyDictionary<string, object?>? Arguments)> Calls { get; } = [];

        public void Enqueue(CallToolResult result) => _results.Enqueue(result);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<CallToolResult> CallToolAsync(
            string name,
            IReadOnlyDictionary<string, object?>? arguments,
            CancellationToken cancellationToken)
        {
            Calls.Add((name, arguments));
            return ValueTask.FromResult(_results.Dequeue());
        }
    }

    /// <summary>Verifies that a successful snapshot writes sessionId and snapshot path.</summary>
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
            Assert.Contains("sessionId s3", stdout);
            var snapshotLine = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Single(l => l.StartsWith("snapshot ", StringComparison.Ordinal));
            var snapshotPath = snapshotLine["snapshot ".Length..];
            Assert.True(File.Exists(Path.GetFullPath(snapshotPath)), snapshotPath);
            Assert.Contains("filter: operable", File.ReadAllText(Path.GetFullPath(snapshotPath)));

            var call = Assert.Single(client.Calls);
            Assert.Equal("windows_snapshot", call.Name);
            Assert.Null(call.Arguments);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Verifies that an unknown filter returns a user error before calling MCP.</summary>
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
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("error INVALID_ARGUMENT", stderr);
        Assert.Empty(client.Calls);
    }

    /// <summary>Verifies that a windows_snapshot MCP error is reported as a CLI error.</summary>
    [Fact]
    public async Task WriteSnapshotResultAsync_SnapshotError_PropagatesMcpError()
    {
        var client = new FakeClient();
        client.Enqueue(ErrorResult("SNAPSHOT_FAILED", "snapshot failed"));

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.WriteSnapshotResultAsync(client, "s1", snapshotDir: null, CancellationToken.None));

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("error SNAPSHOT_FAILED", stderr);
        Assert.Contains("message snapshot failed", stderr);
        Assert.Equal("windows_snapshot", Assert.Single(client.Calls).Name);
    }

    /// <summary>Verifies that a snapshot response without a session id returns an internal error.</summary>
    [Fact]
    public async Task WriteSnapshotResultAsync_MissingSessionId_ReturnsInternalError()
    {
        var client = new FakeClient();
        client.Enqueue(SnapshotResult(sessionId: null));

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.WriteSnapshotResultAsync(client, sessionId: null, snapshotDir: null, CancellationToken.None));

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("error INTERNAL_ERROR", stderr);
        Assert.Contains("windows_snapshot response missing sessionId", stderr);
    }

    /// <summary>Verifies that invalid snapshot JSON text returns an internal parse error.</summary>
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
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("error INTERNAL_ERROR", stderr);
        Assert.Contains("Failed to parse snapshot response", stderr);
    }

    /// <summary>Verifies that an explicit raw filter is passed through to the snapshot text output.</summary>
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
                .Single(l => l.StartsWith("snapshot ", StringComparison.Ordinal));
            var text = File.ReadAllText(Path.GetFullPath(snapshotLine["snapshot ".Length..]));
            Assert.Contains("filter: raw", text);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Verifies that no-snapshot ref operations write only the session id.</summary>
    [Fact]
    public async Task RunRefOperationAndAutoSnapshotAsync_NoSnapshot_WritesSessionFromElementRef()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                client,
                "windows_click",
                new Dictionary<string, object?> { ["ref"] = "s7e9" },
                "s7e9",
                noSnapshot: true,
                snapshotDir: null,
                CancellationToken.None));

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("sessionId s7" + Environment.NewLine, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = Assert.Single(client.Calls);
        Assert.Equal("windows_click", call.Name);
        Assert.Equal("s7e9", call.Arguments!["ref"]);
    }

    /// <summary>Verifies that a failed ref operation skips the follow-up snapshot.</summary>
    [Fact]
    public async Task RunRefOperationAndAutoSnapshotAsync_OperationError_SkipsSnapshot()
    {
        var client = new FakeClient();
        client.Enqueue(ErrorResult("ELEMENT_INTERACTION_FAILED", "click failed"));

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                client,
                "windows_click",
                new Dictionary<string, object?> { ["ref"] = "s1e2" },
                "s1e2",
                noSnapshot: false,
                snapshotDir: null,
                CancellationToken.None));

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("error ELEMENT_INTERACTION_FAILED", stderr);
        Assert.Equal("windows_click", Assert.Single(client.Calls).Name);
    }

    /// <summary>Verifies that a successful session operation snapshots the same session.</summary>
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
                    "windows_resize",
                    new Dictionary<string, object?> { ["sessionId"] = "s4", ["width"] = 800, ["height"] = 600 },
                    "s4",
                    noSnapshot: false,
                    dir,
                    CancellationToken.None));

            Assert.Equal(ExitCodes.Success, exit);
            Assert.Equal(string.Empty, stderr);
            Assert.Contains("sessionId s4", stdout);
            Assert.Equal(["windows_resize", "windows_snapshot"], client.Calls.Select(c => c.Name));
            Assert.Equal("s4", client.Calls[1].Arguments!["sessionId"]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Verifies that a failed session operation skips the follow-up snapshot.</summary>
    [Fact]
    public async Task RunSessionOperationAndAutoSnapshotAsync_OperationError_SkipsSnapshot()
    {
        var client = new FakeClient();
        client.Enqueue(ErrorResult("WINDOW_NOT_FOUND", "window missing"));

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.RunSessionOperationAndAutoSnapshotAsync(
                client,
                "windows_resize",
                new Dictionary<string, object?> { ["sessionId"] = "s2" },
                "s2",
                noSnapshot: false,
                snapshotDir: null,
                CancellationToken.None));

        Assert.Equal(ExitCodes.CommandFailed, exit);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("error WINDOW_NOT_FOUND", stderr);
        Assert.Equal("windows_resize", Assert.Single(client.Calls).Name);
    }

    /// <summary>Verifies that no-snapshot session operations write the explicit session id.</summary>
    [Fact]
    public async Task RunSessionOperationAndAutoSnapshotAsync_NoSnapshot_WritesSessionId()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await CaptureAsync(() =>
            CommandHelpers.RunSessionOperationAndAutoSnapshotAsync(
                client,
                "windows_maximize",
                new Dictionary<string, object?> { ["sessionId"] = "s5" },
                "s5",
                noSnapshot: true,
                snapshotDir: null,
                CancellationToken.None));

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("sessionId s5" + Environment.NewLine, stdout);
        Assert.Equal(string.Empty, stderr);
        Assert.Equal("windows_maximize", Assert.Single(client.Calls).Name);
    }

    /// <summary>Verifies that invalid server arguments fail before connecting.</summary>
    [Fact]
    public async Task RunWithClientAsync_InvalidServer_ReturnsUserErrorWithoutConnecting()
    {
        var connected = false;
        var originalConnect = CommandHelpers.ConnectClientAsync;
        try
        {
            CommandHelpers.ConnectClientAsync = (_, _) =>
            {
                connected = true;
                return Task.FromResult<IAdactMcpClient>(new FakeClient());
            };

            var (stdout, stderr, exit) = await CaptureAsync(() =>
                CommandHelpers.RunWithClientAsync(
                    "ftp://localhost/mcp",
                    (_, _) => Task.FromResult(ExitCodes.Success),
                    CancellationToken.None));

            Assert.Equal(ExitCodes.UserError, exit);
            Assert.False(connected);
            Assert.Equal(string.Empty, stdout);
            Assert.Contains("error INVALID_ARGUMENT", stderr);
        }
        finally
        {
            CommandHelpers.ConnectClientAsync = originalConnect;
        }
    }

    /// <summary>Verifies that connection failures are reported as CONNECTION_FAILED.</summary>
    [Fact]
    public async Task RunWithClientAsync_ConnectionFailure_ReturnsConnectionFailed()
    {
        var originalConnect = CommandHelpers.ConnectClientAsync;
        try
        {
            CommandHelpers.ConnectClientAsync = (_, _) => throw new HttpRequestException("daemon unavailable");

            var (stdout, stderr, exit) = await CaptureAsync(() =>
                CommandHelpers.RunWithClientAsync(
                    "http://127.0.0.1:41300/mcp",
                    (_, _) => Task.FromResult(ExitCodes.Success),
                    CancellationToken.None));

            Assert.Equal(ExitCodes.ConnectionFailed, exit);
            Assert.Equal(string.Empty, stdout);
            Assert.Contains("error CONNECTION_FAILED", stderr);
            Assert.Contains("daemon unavailable", stderr);
        }
        finally
        {
            CommandHelpers.ConnectClientAsync = originalConnect;
        }
    }

    /// <summary>Verifies that unexpected connector exceptions are reported as INTERNAL_ERROR.</summary>
    [Fact]
    public async Task RunWithClientAsync_UnexpectedConnectorException_ReturnsCommandFailed()
    {
        var originalConnect = CommandHelpers.ConnectClientAsync;
        try
        {
            CommandHelpers.ConnectClientAsync = (_, _) => throw new InvalidOperationException("boom");

            var (stdout, stderr, exit) = await CaptureAsync(() =>
                CommandHelpers.RunWithClientAsync(
                    "http://127.0.0.1:41300/mcp",
                    (_, _) => Task.FromResult(ExitCodes.Success),
                    CancellationToken.None));

            Assert.Equal(ExitCodes.CommandFailed, exit);
            Assert.Equal(string.Empty, stdout);
            Assert.Contains("error INTERNAL_ERROR", stderr);
            Assert.Contains("boom", stderr);
        }
        finally
        {
            CommandHelpers.ConnectClientAsync = originalConnect;
        }
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
