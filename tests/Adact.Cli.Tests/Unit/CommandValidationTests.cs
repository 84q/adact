using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Commands;
using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// コマンドの引数バリデーションロジックを検証する Unit テスト。
/// 不正な引数が UserError exit code を返すことを確認する。
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class CommandValidationTests
{
    private sealed class FakeClient : IAdactMcpClient
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<CallToolResult> CallToolAsync(
            string name,
            IReadOnlyDictionary<string, object?>? arguments,
            CancellationToken cancellationToken)
        {
            // バリデーションで弾かれるため呼ばれないはず
            throw new InvalidOperationException("CallToolAsync should not be called in validation error tests.");
        }
    }

    // --- ClickCommand ---

    [Theory]
    [InlineData("badref")]
    [InlineData("s1")]
    [InlineData("")]
    [InlineData("S1E2")]
    public async Task Click_InvalidRefFormat_ReturnsUserError(string badRef)
    {
        var (_, _, exit) = await RunAsync(ClickCommand.Build(), ["click", badRef, "--no-snapshot"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    // --- FillCommand ---

    [Theory]
    [InlineData("badref")]
    [InlineData("w1")]
    public async Task Fill_InvalidRefFormat_ReturnsUserError(string badRef)
    {
        var (_, _, exit) = await RunAsync(FillCommand.Build(), ["fill", badRef, "text", "--no-snapshot"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    // --- SelectCommand ---

    [Fact]
    public async Task Select_InvalidRefFormat_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(SelectCommand.Build(), ["select", "badref", "--name", "A"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    [Fact]
    public async Task Select_NoSelectorSpecified_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(SelectCommand.Build(), ["select", "s1e2"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    [Fact]
    public async Task Select_MultipleSelectorKinds_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(SelectCommand.Build(), ["select", "s1e2", "--name", "A", "--index", "0"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    [Fact]
    public async Task Select_InvalidItemRef_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(SelectCommand.Build(), ["select", "s1e2", "--item-ref", "badref"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    [Fact]
    public async Task Select_AddAndRemove_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(SelectCommand.Build(), ["select", "s1e2", "--name", "A", "--add", "--remove"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    // --- TypeCommand ---

    [Theory]
    [InlineData("badref")]
    [InlineData("s1")]
    public async Task Type_InvalidRefFormat_ReturnsUserError(string badRef)
    {
        var (_, _, exit) = await RunAsync(TypeCommand.Build(), ["type", badRef, "hello"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    // --- MousewheelCommand ---

    [Fact]
    public async Task Mousewheel_BothDeltaZero_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(MousewheelCommand.Build(), ["mousewheel", "--delta-x", "0", "--delta-y", "0"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    [Fact]
    public async Task Mousewheel_NoDeltaSpecified_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(MousewheelCommand.Build(), ["mousewheel"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    [Fact]
    public async Task Type_NegativeDelay_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(TypeCommand.Build(), ["type", "s1e2", "hello", "--delay-ms", "-1"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    // --- ClickCommand extended options ---

    [Fact]
    public async Task Click_InvalidButton_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(ClickCommand.Build(), ["click", "s1e2", "--button", "invalid", "--no-snapshot"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    [Fact]
    public async Task Click_InvalidPosition_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(ClickCommand.Build(), ["click", "s1e2", "--position", "abc", "--no-snapshot"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    [Fact]
    public async Task Click_CountLessThanOne_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(ClickCommand.Build(), ["click", "s1e2", "--count", "0", "--no-snapshot"]);
        Assert.Equal(ExitCodes.UserError, exit);
    }

    // --- helper ---

    private static async Task<(string stdout, string stderr, int exit)> RunAsync(
        Command command,
        string[] args)
    {
        var origOut = Console.Out;
        var origErr = Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        using var _ = CommandHelpers.PushRuntime(new CommandHelpers.CommandRuntime(
            ConnectHttpClientAsync: (_, _) => Task.FromResult<IAdactMcpClient>(new FakeClient()),
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
