using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Commands;
using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class WaitAndScreenshotSuccessTests
{
    private sealed class FakeClient : IAdactMcpClient
    {
        private readonly Queue<CallToolResult> _results = new();

        public void Enqueue(CallToolResult result) => _results.Enqueue(result);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<CallToolResult> CallToolAsync(
            string name,
            IReadOnlyDictionary<string, object?>? arguments,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(_results.Dequeue());
    }

    [Fact]
    public async Task Screenshot_Success_WritesResolvedSessionIdInBody()
    {
        var client = new FakeClient();
        client.Enqueue(JsonResult(new { sessionId = "s3", path = "shot.png", width = 100, height = 50 }));

        var (stdout, stderr, exit) = await RunWithClientAsync(client, ScreenshotCommand.Build(),
            ["screenshot", "--ref", "s3e7"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("sessionId: s3", stdout);
        Assert.Contains("path: shot.png", stdout);
    }

    [Fact]
    public async Task WaitFor_Success_WritesSessionIdInBody()
    {
        var client = new FakeClient();
        client.Enqueue(JsonResult(new { sessionId = "s4", @ref = "s4e9", state = "visible" }));

        var (stdout, stderr, exit) = await RunWithClientAsync(client, WaitForCommand.Build(),
            ["wait-for", "--ref", "s4e9"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("sessionId: s4", stdout);
        Assert.Contains("ref: s4e9", stdout);
        Assert.Contains("state: visible", stdout);
    }

    private static CallToolResult JsonResult(object value) => new()
    {
        Content = [],
        StructuredContent = JsonSerializer.SerializeToElement(value),
    };

    private static async Task<(string stdout, string stderr, int exit)> RunWithClientAsync(
        FakeClient client,
        Command command,
        string[] args)
    {
        var origOut = Console.Out;
        var origErr = Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        using var _ = CommandHelpers.PushRuntime(new CommandHelpers.CommandRuntime(
            ConnectHttpClientAsync: (_, _) => Task.FromResult<IAdactMcpClient>(client),
            ConnectNamedPipeClientAsync: static async (endpoint, ct) => await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false),
            IsServerRunningAsync: NamedPipeMcpClient.IsServerRunningAsync,
            TryAutoStartServerAsync: null));

        try
        {
            Console.SetOut(outWriter);
            Console.SetError(errWriter);

            var root = new RootCommand("test");
            root.Options.Add(CommandHelpers.ServerOption);
            root.Subcommands.Add(command);

            var argsWithServer = new List<string> { "--server", "http://localhost:41300/mcp" };
            argsWithServer.AddRange(args);
            var exit = await root.Parse(argsWithServer.ToArray()).InvokeAsync().ConfigureAwait(false);
            return (outWriter.ToString(), errWriter.ToString(), exit);
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }
}
