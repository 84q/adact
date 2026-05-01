using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Commands;
using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// Unit tests for successful CLI action command argument mapping into MCP tool calls.
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class CommandActionSuccessTests
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

    /// <summary>Verifies that click maps extended options into windows_click arguments.</summary>
    [Fact]
    public async Task Click_Success_MapsOptionsToWindowsClick()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            ClickCommand.Build(),
            [
                "click", "s2e5",
                "--button", "right",
                "--count", "2",
                "--modifier", "Ctrl",
                "--modifier", "Shift",
                "--position", "10,20",
                "--no-snapshot",
            ]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("sessionId s2" + Environment.NewLine, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = Assert.Single(client.Calls);
        Assert.Equal("windows_click", call.Name);
        Assert.Equal("s2e5", call.Arguments!["ref"]);
        Assert.Equal("right", call.Arguments["button"]);
        Assert.Equal(2, call.Arguments["count"]);
        Assert.Equal(new[] { "Ctrl", "Shift" }, Assert.IsType<string[]>(call.Arguments["modifiers"]));
        Assert.Equal(10, call.Arguments["positionX"]);
        Assert.Equal(20, call.Arguments["positionY"]);
    }

    /// <summary>Verifies that fill maps ref and value into windows_fill arguments.</summary>
    [Fact]
    public async Task Fill_Success_MapsTextToWindowsFill()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            FillCommand.Build(),
            ["fill", "s3e4", "hello world", "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("sessionId s3" + Environment.NewLine, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = Assert.Single(client.Calls);
        Assert.Equal("windows_fill", call.Name);
        Assert.Equal("s3e4", call.Arguments!["ref"]);
        Assert.Equal("hello world", call.Arguments["value"]);
    }

    /// <summary>Verifies that type maps text and delay into windows_type arguments.</summary>
    [Fact]
    public async Task Type_Success_MapsDelayToWindowsType()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            TypeCommand.Build(),
            ["type", "s4e8", "abc", "--delay-ms", "15", "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("sessionId s4" + Environment.NewLine, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = Assert.Single(client.Calls);
        Assert.Equal("windows_type", call.Name);
        Assert.Equal("s4e8", call.Arguments!["ref"]);
        Assert.Equal("abc", call.Arguments["text"]);
        Assert.Equal(15, call.Arguments["delayMs"]);
    }

    /// <summary>Verifies that resize maps dimensions and session id into windows_resize arguments.</summary>
    [Fact]
    public async Task Resize_Success_MapsDimensionsToWindowsResize()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            ResizeCommand.Build(),
            ["resize", "--width", "800", "--height", "600", "--sid", "s9", "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("sessionId s9" + Environment.NewLine, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = Assert.Single(client.Calls);
        Assert.Equal("windows_resize", call.Name);
        Assert.Equal(800, call.Arguments!["width"]);
        Assert.Equal(600, call.Arguments["height"]);
        Assert.Equal("s9", call.Arguments["sessionId"]);
    }

    /// <summary>Verifies that dblclick maps button, modifiers, and position into windows_dblclick.</summary>
    [Fact]
    public async Task Dblclick_Success_MapsOptionsToWindowsDblclick()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            DblclickCommand.Build(),
            [
                "dblclick", "s5e6",
                "--button", "middle",
                "--modifier", "Alt",
                "--position", "3,4",
                "--no-snapshot",
            ]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("sessionId s5" + Environment.NewLine, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, "windows_dblclick");
        Assert.Equal("s5e6", call["ref"]);
        Assert.Equal("middle", call["button"]);
        Assert.Equal(new[] { "Alt" }, Assert.IsType<string[]>(call["modifiers"]));
        Assert.Equal(3, call["positionX"]);
        Assert.Equal(4, call["positionY"]);
    }

    /// <summary>Verifies that hover maps modifiers and position into windows_hover.</summary>
    [Fact]
    public async Task Hover_Success_MapsOptionsToWindowsHover()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            HoverCommand.Build(),
            ["hover", "s6e7", "--modifier", "Shift", "--position", "8,9", "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("sessionId s6" + Environment.NewLine, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, "windows_hover");
        Assert.Equal("s6e7", call["ref"]);
        Assert.Equal(new[] { "Shift" }, Assert.IsType<string[]>(call["modifiers"]));
        Assert.Equal(8, call["positionX"]);
        Assert.Equal(9, call["positionY"]);
    }

    /// <summary>Verifies that select maps the chosen selector into windows_select.</summary>
    [Theory]
    [InlineData("--name", "Option A", "name", "Option A")]
    [InlineData("--index", "2", "index", 2)]
    [InlineData("--item-ref", "s7e9", "itemRef", "s7e9")]
    public async Task Select_Success_MapsSelectorToWindowsSelect(
        string option,
        string value,
        string expectedKey,
        object expectedValue)
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            SelectCommand.Build(),
            ["select", "s7e3", option, value, "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("sessionId s7" + Environment.NewLine, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, "windows_select");
        Assert.Equal("s7e3", call["ref"]);
        Assert.Equal(expectedValue, call[expectedKey]);
    }

    /// <summary>Verifies ref-only auto-snapshot commands map ref to the expected tool.</summary>
    [Theory]
    [MemberData(nameof(RefOnlyAutoSnapshotCommands))]
    public async Task RefOnlyAutoSnapshotCommands_Success_MapRefToExpectedTool(Command command, string toolName)
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            command,
            [command.Name, "s8e4", "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("sessionId s8" + Environment.NewLine, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, toolName);
        Assert.Equal("s8e4", call["ref"]);
    }

    /// <summary>Verifies ref-only low-level commands map ref to the expected tool without snapshot output.</summary>
    [Theory]
    [MemberData(nameof(RefOnlyLowLevelCommands))]
    public async Task RefOnlyLowLevelCommands_Success_MapRefToExpectedTool(Command command, string toolName)
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            command,
            [command.Name, "s9e4"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, toolName);
        Assert.Equal("s9e4", call["ref"]);
    }

    /// <summary>Verifies low-level mouse commands map target and button options.</summary>
    [Theory]
    [MemberData(nameof(MouseLowLevelCommands))]
    public async Task MouseLowLevelCommands_Success_MapTargetToExpectedTool(
        Command command,
        string toolName,
        string[] args,
        string? expectedButton)
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(client, command, args);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, toolName);
        Assert.Equal("10,20", call["target"]);
        if (expectedButton is not null)
        {
            Assert.Equal(expectedButton, call["button"]);
        }
    }

    /// <summary>Verifies that mouse-wheel maps deltas into windows_mouse_wheel.</summary>
    [Fact]
    public async Task MouseWheel_Success_MapsDeltasToWindowsMouseWheel()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            MouseWheelCommand.Build(),
            ["mouse-wheel", "s10e2", "--delta-x", "-1", "--delta-y", "3", "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, "windows_mouse_wheel");
        Assert.Equal("s10e2", call["target"]);
        Assert.Equal(-1, call["deltaX"]);
        Assert.Equal(3, call["deltaY"]);
    }

    /// <summary>Verifies that press maps key and optional ref into windows_press.</summary>
    [Fact]
    public async Task Press_Success_MapsKeyAndRefToWindowsPress()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            PressCommand.Build(),
            ["press", "Ctrl+Shift+E", "--ref", "s11e5", "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, "windows_press");
        Assert.Equal("Ctrl+Shift+E", call["key"]);
        Assert.Equal("s11e5", call["ref"]);
    }

    /// <summary>Verifies key down/up map key names into the expected tools.</summary>
    [Theory]
    [MemberData(nameof(KeyCommands))]
    public async Task KeyCommands_Success_MapKeyToExpectedTool(Command command, string toolName)
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            command,
            [command.Name, "Shift"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal(string.Empty, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, toolName);
        Assert.Equal("Shift", call["key"]);
    }

    /// <summary>Verifies window-state commands map session id into the expected tools.</summary>
    [Theory]
    [MemberData(nameof(WindowStateCommands))]
    public async Task WindowStateCommands_Success_MapSessionIdToExpectedTool(Command command, string toolName)
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            command,
            [command.Name, "--sid", "s12", "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Equal("sessionId s12" + Environment.NewLine, stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, toolName);
        Assert.Equal("s12", call["sessionId"]);
    }

    private static CallToolResult SuccessResult() => new()
    {
        Content = [],
        StructuredContent = JsonSerializer.SerializeToElement(new { }),
    };

    /// <summary>Provides auto-snapshot ref-only command builders and expected MCP tool names.</summary>
    public static IEnumerable<object[]> RefOnlyAutoSnapshotCommands()
    {
        yield return [CheckCommand.Build(), "windows_check"];
        yield return [UncheckCommand.Build(), "windows_uncheck"];
        yield return [ClearCommand.Build(), "windows_clear"];
    }

    /// <summary>Provides low-level ref-only command builders and expected MCP tool names.</summary>
    public static IEnumerable<object[]> RefOnlyLowLevelCommands()
    {
        yield return [FocusCommand.Build(), "windows_focus"];
        yield return [ScrollIntoViewCommand.Build(), "windows_scroll_into_view"];
    }

    /// <summary>Provides low-level mouse command builders, CLI args, and expected MCP tool names.</summary>
    public static IEnumerable<object[]> MouseLowLevelCommands()
    {
        yield return [MouseMoveCommand.Build(), "windows_mouse_move", new[] { "mouse-move", "10,20" }, null!];
        yield return [MouseDownCommand.Build(), "windows_mouse_down", new[] { "mouse-down", "10,20", "--button", "right" }, "right"];
        yield return [MouseUpCommand.Build(), "windows_mouse_up", new[] { "mouse-up", "10,20", "--button", "middle" }, "middle"];
    }

    /// <summary>Provides key command builders and expected MCP tool names.</summary>
    public static IEnumerable<object[]> KeyCommands()
    {
        yield return [KeyDownCommand.Build(), "windows_key_down"];
        yield return [KeyUpCommand.Build(), "windows_key_up"];
    }

    /// <summary>Provides window-state command builders and expected MCP tool names.</summary>
    public static IEnumerable<object[]> WindowStateCommands()
    {
        yield return [MinimizeCommand.Build(), "windows_minimize"];
        yield return [MaximizeCommand.Build(), "windows_maximize"];
        yield return [RestoreCommand.Build(), "windows_restore"];
    }

    private static IReadOnlyDictionary<string, object?> AssertSingleCall(FakeClient client, string name)
    {
        var call = Assert.Single(client.Calls);
        Assert.Equal(name, call.Name);
        Assert.NotNull(call.Arguments);
        return call.Arguments;
    }

    private static async Task<(string stdout, string stderr, int exit)> RunWithClientAsync(
        FakeClient client,
        Command command,
        string[] args)
    {
        var originalConnect = CommandHelpers.ConnectClientAsync;
        var origOut = Console.Out;
        var origErr = Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        try
        {
            CommandHelpers.ConnectClientAsync = (_, _) => Task.FromResult<IAdactMcpClient>(client);
            Console.SetOut(outWriter);
            Console.SetError(errWriter);

            var root = new RootCommand("test");
            root.Subcommands.Add(command);
            var exit = await root.Parse(args).InvokeAsync().ConfigureAwait(false);
            return (outWriter.ToString(), errWriter.ToString(), exit);
        }
        finally
        {
            CommandHelpers.ConnectClientAsync = originalConnect;
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }
}
