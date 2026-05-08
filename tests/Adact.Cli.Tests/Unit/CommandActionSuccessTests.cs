using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Commands;
using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Command Action Success behavior.</summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class CommandActionSuccessTests
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

    /// <summary>Performs the Click Success Maps Options To Windows Click operation.</summary>
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
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("target:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = Assert.Single(client.Calls);
        Assert.Equal("adact_click", call.Name);
        Assert.Equal("s2e5", call.Arguments!["ref"]);
        Assert.Equal("right", call.Arguments["button"]);
        Assert.Equal(2, call.Arguments["count"]);
        Assert.Equal(new[] { "Ctrl", "Shift" }, Assert.IsType<string[]>(call.Arguments["modifiers"]));
        Assert.Equal(10, call.Arguments["positionX"]);
        Assert.Equal(20, call.Arguments["positionY"]);
    }

    /// <summary>Performs the Fill Success Maps Text To Windows Fill operation.</summary>
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
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("target:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = Assert.Single(client.Calls);
        Assert.Equal("adact_fill", call.Name);
        Assert.Equal("s3e4", call.Arguments!["ref"]);
        Assert.Equal("hello world", call.Arguments["value"]);
    }

    /// <summary>Performs the Type Success Maps Delay To Windows Type operation.</summary>
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
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("target:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = Assert.Single(client.Calls);
        Assert.Equal("adact_type", call.Name);
        Assert.Equal("s4e8", call.Arguments!["ref"]);
        Assert.Equal("abc", call.Arguments["text"]);
        Assert.Equal(15, call.Arguments["delayMs"]);
    }

    /// <summary>Performs the Resize Success Maps Dimensions To Windows Resize operation.</summary>
    [Fact]
    public async Task Resize_Success_MapsDimensionsToWindowsResize()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            ResizeWindowCommand.Build(),
            ["resize-window", "s9", "--width", "800", "--height", "600", "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("sessionId:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = Assert.Single(client.Calls);
        Assert.Equal("adact_resize_window", call.Name);
        Assert.Equal(800, call.Arguments!["width"]);
        Assert.Equal(600, call.Arguments["height"]);
        Assert.Equal("s9", call.Arguments["sessionId"]);
    }

    /// <summary>Performs the Dblclick Success Maps Options To Windows Dblclick operation.</summary>
    [Fact]
    public async Task Dblclick_Success_MapsOptionsToWindowsDblclick()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            DoubleclickCommand.Build(),
            [
                "doubleclick", "s5e6",
                "--button", "middle",
                "--modifier", "Alt",
                "--position", "3,4",
                "--no-snapshot",
            ]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("target:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, "adact_doubleclick");
        Assert.Equal("s5e6", call["ref"]);
        Assert.Equal("middle", call["button"]);
        Assert.Equal(new[] { "Alt" }, Assert.IsType<string[]>(call["modifiers"]));
        Assert.Equal(3, call["positionX"]);
        Assert.Equal(4, call["positionY"]);
    }

    /// <summary>Performs the Hover Success Maps Options To Windows Hover operation.</summary>
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
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("target:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, "adact_hover");
        Assert.Equal("s6e7", call["ref"]);
        Assert.Equal(new[] { "Shift" }, Assert.IsType<string[]>(call["modifiers"]));
        Assert.Equal(8, call["positionX"]);
        Assert.Equal(9, call["positionY"]);
    }

    /// <summary>Performs the Select Success Maps Selector To Windows Select operation.</summary>
    [Theory]
    [InlineData("--name", "Option A", "name")]
    [InlineData("--index", "2", "index")]
    [InlineData("--item-ref", "s7e9", "itemRef")]
    public async Task Select_Success_MapsSelectorToWindowsSelect(
        string option,
        string value,
        string expectedKey)
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            SelectCommand.Build(),
            ["select", "s7e3", option, value, "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("target:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, "adact_select");
        Assert.Equal("s7e3", call["ref"]);
        Assert.True(call.ContainsKey(expectedKey));
    }

    /// <summary>Performs the Ref Only Auto Snapshot Commands Success Map Ref To Expected Tool operation.</summary>
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
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("target:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, toolName);
        Assert.Equal("s8e4", call["ref"]);
    }

    /// <summary>Performs the Ref Only Low Level Commands Success Map Ref To Expected Tool operation.</summary>
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
        Assert.Contains($"action: {command.Name}", stdout);
        Assert.Contains("target: s9e4", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, toolName);
        Assert.Equal("s9e4", call["ref"]);
    }

    /// <summary>Performs the Mouse Low Level Commands Success Map Target To Expected Tool operation.</summary>
    [Theory]
    [MemberData(nameof(MouseLowLevelCommands))]
    public async Task MouseLowLevelCommands_Success_MapTargetToExpectedTool(
        Command command,
        string toolName,
        string[] args,
        string expectedTarget,
        string? expectedButton)
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(client, command, args);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Contains("result: true", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("target:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, toolName);
        if (!string.IsNullOrEmpty(expectedTarget))
        {
            Assert.Equal(expectedTarget, call["target"]);
        }
        else
        {
            Assert.DoesNotContain("target", call.Keys);
        }
        if (expectedButton is not null)
        {
            Assert.Equal(expectedButton, call["button"]);
        }
    }

    /// <summary>Performs the Mouse Wheel Success Maps Deltas To Windows Mouse Wheel operation.</summary>
    [Fact]
    public async Task MouseWheel_Success_MapsDeltasToWindowsMouseWheel()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            MousewheelCommand.Build(),
            ["mousewheel", "--delta-x", "-1", "--delta-y", "3"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("target:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, "adact_mousewheel");
        Assert.DoesNotContain("target", call.Keys);
        Assert.Equal(-1, call["deltaX"]);
        Assert.Equal(3, call["deltaY"]);
    }

    /// <summary>Performs the Press Success Maps Key To Windows Press operation.</summary>
    [Fact]
    public async Task Press_Success_MapsKeyToWindowsPress()
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            KeypressCommand.Build(),
            ["keypress", "Ctrl+Shift+E"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("target:", stdout);
        Assert.DoesNotContain("key:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, "adact_keypress");
        Assert.Equal("Ctrl+Shift+E", call["key"]);
        Assert.DoesNotContain("ref", call.Keys);
    }

    /// <summary>Performs the Key Commands Success Map Key To Expected Tool operation.</summary>
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
        Assert.Contains("result: true", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("key:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, toolName);
        Assert.Equal("Shift", call["key"]);
    }

    /// <summary>Performs the Window State Commands Success Map Session Id To Expected Tool operation.</summary>
    [Theory]
    [MemberData(nameof(WindowStateCommands))]
    public async Task WindowStateCommands_Success_MapSessionIdToExpectedTool(Command command, string toolName)
    {
        var client = new FakeClient();
        client.Enqueue(SuccessResult());

        var (stdout, stderr, exit) = await RunWithClientAsync(
            client,
            command,
            [command.Name, "s12", "--no-snapshot"]);

        Assert.Equal(ExitCodes.Success, exit);
        Assert.Contains("result: true", stdout);
        Assert.Contains("---", stdout);
        Assert.DoesNotContain("action:", stdout);
        Assert.DoesNotContain("sessionId:", stdout);
        Assert.Equal(string.Empty, stderr);
        var call = AssertSingleCall(client, toolName);
        Assert.Equal("s12", call["sessionId"]);
    }

    private static CallToolResult SuccessResult() => new()
    {
        Content = [],
        StructuredContent = JsonSerializer.SerializeToElement(new { }),
    };

    /// <summary>Performs the Ref Only Auto Snapshot Commands operation.</summary>
    public static IEnumerable<object[]> RefOnlyAutoSnapshotCommands()
    {
        yield return [CheckCommand.Build(), "adact_check"];
        yield return [UncheckCommand.Build(), "adact_uncheck"];
    }

    /// <summary>Performs the Ref Only Low Level Commands operation.</summary>
    public static IEnumerable<object[]> RefOnlyLowLevelCommands()
    {
        yield return [FocusCommand.Build(), "adact_focus"];
        yield return [ScrollIntoViewCommand.Build(), "adact_scroll_into_view"];
    }

    /// <summary>Performs the Mouse Low Level Commands operation.</summary>
    public static IEnumerable<object[]> MouseLowLevelCommands()
    {
        yield return [MousemoveCommand.Build(), "adact_mousemove", new[] { "mousemove", "10,20" }, "10,20", null!];
        yield return [MousedownCommand.Build(), "adact_mousedown", new[] { "mousedown", "--button", "right" }, string.Empty, "right"];
        yield return [MouseupCommand.Build(), "adact_mouseup", new[] { "mouseup", "--button", "middle" }, string.Empty, "middle"];
    }

    /// <summary>Performs the Key Commands operation.</summary>
    public static IEnumerable<object[]> KeyCommands()
    {
        yield return [KeydownCommand.Build(), "adact_keydown"];
        yield return [KeyupCommand.Build(), "adact_keyup"];
    }

    /// <summary>Performs the Window State Commands operation.</summary>
    public static IEnumerable<object[]> WindowStateCommands()
    {
        yield return [MinimizeWindowCommand.Build(), "adact_minimize_window"];
        yield return [MaximizeWindowCommand.Build(), "adact_maximize_window"];
        yield return [RestoreWindowCommand.Build(), "adact_restore_window"];
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
