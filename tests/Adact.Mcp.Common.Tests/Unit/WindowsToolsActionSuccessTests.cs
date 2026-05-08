using Adact.Engine;
using Adact.Engine.Snapshot;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>Contains tests for the Windows Tools Action Success behavior.</summary>
[Trait("Layer", "Unit")]
public class WindowsToolsActionSuccessTests
{
    private sealed class FakeDaemonControl : IDaemonControl
    {
        /// <summary>Gets a value indicating whether Is Supported.</summary>
        public bool IsSupported => true;
        /// <summary>Performs the Stop Async operation.</summary>
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeWindowSession : IWindowSession
    {
        /// <summary>Gets the Calls value.</summary>
        public List<string> Calls { get; } = [];
        /// <summary>Gets the Session Id value.</summary>
        public int SessionId { get; init; } = 1;
        /// <summary>Gets the Process Name value.</summary>
        public string ProcessName { get; init; } = "fake";
        /// <summary>Gets the Process Id value.</summary>
        public int ProcessId { get; init; } = 1234;
        /// <summary>Gets the Title value.</summary>
        public string Title { get; init; } = "Fake";
        /// <summary>Gets the Native Window Handle value.</summary>
        public nint NativeWindowHandle { get; init; } = 0x1234;
        /// <summary>Gets or sets the Disposed value.</summary>
        public bool Disposed { get; private set; }

        /// <summary>Performs the Snapshot Async operation.</summary>
        public Task<SnapshotResult> SnapshotAsync(SnapshotOptions? options = null, CancellationToken ct = default)
            => Task.FromResult(new SnapshotResult("{}", "s1", Title, ProcessName, ProcessId, DateTimeOffset.UtcNow));

        /// <summary>Performs the Click Async operation.</summary>
        public Task ClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default)
        {
            Calls.Add($"click:{refId}:{(options is null ? "null" : "options")}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Click With Options Async operation.</summary>
        public Task ClickWithOptionsAsync(string refId, ClickOptions options, CancellationToken ct = default)
        {
            Calls.Add($"click-options:{refId}:{options.Button}:{options.Count}:{options.PositionX}:{options.PositionY}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Double Click Async operation.</summary>
        public Task DoubleClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default)
        {
            Calls.Add($"doubleclick:{refId}:{options?.Button}:{options?.PositionX}:{options?.PositionY}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Fill Async operation.</summary>
        public Task FillAsync(string refId, string text, CancellationToken ct = default)
        {
            Calls.Add($"fill:{refId}:{text}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Press Async operation.</summary>
        public Task PressAsync(string key, string? refId = null, CancellationToken ct = default)
        {
            Calls.Add($"press:{key}:{refId ?? "<window>"}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Key Down Async operation.</summary>
        public Task KeyDownAsync(string key, CancellationToken ct = default)
        {
            Calls.Add($"key-down:{key}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Key Up Async operation.</summary>
        public Task KeyUpAsync(string key, CancellationToken ct = default)
        {
            Calls.Add($"key-up:{key}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Type Async operation.</summary>
        public Task TypeAsync(string? refId, string text, int delayMs = 0, CancellationToken ct = default)
        {
            Calls.Add($"type:{refId}:{text}:{delayMs}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Hover Async operation.</summary>
        public Task HoverAsync(string refId, IReadOnlyList<string>? modifiers = null, int? positionX = null, int? positionY = null, CancellationToken ct = default)
        {
            Calls.Add($"hover:{refId}:{positionX}:{positionY}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Mouse Move Async operation.</summary>
        public Task MouseMoveAsync(MouseTarget target, CancellationToken ct = default)
        {
            Calls.Add($"mousemove:{Describe(target)}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Mouse Down Async operation.</summary>
        public Task MouseDownAsync(MouseTarget target, MouseButton button, CancellationToken ct = default)
        {
            Calls.Add($"mousedown:{Describe(target)}:{button}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Mouse Up Async operation.</summary>
        public Task MouseUpAsync(MouseTarget target, MouseButton button, CancellationToken ct = default)
        {
            Calls.Add($"mouseup:{Describe(target)}:{button}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Mouse Wheel Async operation.</summary>
        public Task MouseWheelAsync(MouseTarget target, int deltaX, int deltaY, CancellationToken ct = default)
        {
            Calls.Add($"mousewheel:{Describe(target)}:{deltaX}:{deltaY}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Check Async operation.</summary>
        public Task CheckAsync(string refId, CancellationToken ct = default)
        {
            Calls.Add($"check:{refId}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Uncheck Async operation.</summary>
        public Task UncheckAsync(string refId, CancellationToken ct = default)
        {
            Calls.Add($"uncheck:{refId}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Select Async operation.</summary>
        public Task SelectAsync(string refId, SelectionTarget[] targets, SelectionMode mode = SelectionMode.Replace, CancellationToken ct = default)
        {
            var targetDesc = string.Join(",", targets.Select(t => t switch
            {
                SelectionTarget.ByName n => n.Name,
                SelectionTarget.ByIndex i => i.Index.ToString(),
                SelectionTarget.ByItemRef r => r.ItemRef,
                _ => "?"
            }));
            Calls.Add($"select:{refId}:{targetDesc}:{mode}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Focus Async operation.</summary>
        public Task FocusAsync(string refId, CancellationToken ct = default)
        {
            Calls.Add($"focus:{refId}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Scroll Into View Async operation.</summary>
        public Task ScrollIntoViewAsync(string refId, CancellationToken ct = default)
        {
            Calls.Add($"scroll:{refId}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Scroll Async operation.</summary>
        public Task ScrollAsync(string refId, ScrollMode mode, CancellationToken ct = default)
        {
            Calls.Add($"scrollPattern:{refId}:{mode}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Inspect Async operation.</summary>
        public Task<InspectResult> InspectAsync(string refId, CancellationToken ct = default)
            => throw new NotSupportedException();

        /// <summary>Performs the Screenshot Async operation.</summary>
        public Task<ScreenshotResult> ScreenshotAsync(string? refId, string? outPath, CancellationToken ct = default)
            => throw new NotSupportedException();

        /// <summary>Performs the Resize Async operation.</summary>
        public Task ResizeAsync(int? width, int? height, CancellationToken ct = default)
        {
            Calls.Add($"resize:{width}:{height}");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Minimize Async operation.</summary>
        public Task MinimizeAsync(CancellationToken ct = default)
        {
            Calls.Add("minimize");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Maximize Async operation.</summary>
        public Task MaximizeAsync(CancellationToken ct = default)
        {
            Calls.Add("maximize");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Restore Async operation.</summary>
        public Task RestoreAsync(CancellationToken ct = default)
        {
            Calls.Add("restore");
            return Task.CompletedTask;
        }

        /// <summary>Waits for the Wait For Ref Async condition.</summary>
        public Task<WaitForResult> WaitForRefAsync(string refId, WaitForState state, TimeSpan timeout, CancellationToken ct = default)
            => throw new NotSupportedException();

        /// <summary>Waits for the Wait For Query Async condition.</summary>
        public Task<WaitForResult> WaitForQueryAsync(WaitForElementQuery query, WaitForState state, TimeSpan timeout, CancellationToken ct = default)
            => throw new NotSupportedException();

        /// <summary>Performs the Close Async operation.</summary>
        public Task CloseAsync(CancellationToken ct = default)
        {
            Calls.Add("close");
            return Task.CompletedTask;
        }

        /// <summary>Performs the Kill Async operation.</summary>
        public Task<KillMethod> KillAsync(bool force = false, int timeoutMs = 5000, CancellationToken ct = default)
        {
            Calls.Add("kill");
            return Task.FromResult(force ? KillMethod.Forced : KillMethod.Graceful);
        }

        /// <summary>Releases resources.</summary>
        public void Dispose() => Disposed = true;

        private static string Describe(MouseTarget target)
            => target switch
            {
                MouseTarget.ByRef r => r.Ref,
                MouseTarget.ByPoint p => $"{p.X},{p.Y}",
                _ => target.ToString() ?? "<unknown>",
            };
    }

    private static (WindowsTools Tools, SessionStore Store, FakeWindowSession Session, FakeMouseDriver Mouse, FakeKeyboardDriver Keyboard) CreateTools()
    {
        var store = new SessionStore(new UiaEngine());
        var session = new FakeWindowSession();
        store.Register(session);
        var mouse = new FakeMouseDriver();
        var keyboard = new FakeKeyboardDriver();
        var tools = new WindowsTools(
            store, new WindowRefStore(), new FakeDaemonControl(),
            mouseDriver: mouse, keyboardDriver: keyboard);
        return (tools, store, session, mouse, keyboard);
    }

    /// <summary>Performs the Click Default Delegates To Session Click operation.</summary>
    [Fact]
    public async Task Click_Default_DelegatesToSessionClick()
    {
        var (tools, store, session, _, _) = CreateTools();
        try
        {
            var result = await tools.ClickAsync("s1e2");

            Assert.True(result.IsError != true);
            Assert.Contains("click:s1e2:null", session.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Click With Options Delegates To Session Click With Options operation.</summary>
    [Fact]
    public async Task Click_WithOptions_DelegatesToSessionClickWithOptions()
    {
        var (tools, store, session, _, _) = CreateTools();
        try
        {
            var result = await tools.ClickAsync("s1e2", button: "right", count: 2, positionX: 3, positionY: 4);

            Assert.True(result.IsError != true);
            Assert.Contains("click-options:s1e2:Right:2:3:4", session.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Fill Delegates To Session operation.</summary>
    [Fact]
    public async Task Fill_DelegatesToSession()
    {
        var (tools, store, session, _, _) = CreateTools();
        try
        {
            var result = await tools.FillAsync("s1e2", "value");

            Assert.True(result.IsError != true);
            Assert.Contains("fill:s1e2:value", session.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Keyboard Actions Delegate To Session operation.</summary>
    [Fact]
    public async Task KeyboardActions_DelegateToSession()
    {
        var (tools, store, session, _, keyboard) = CreateTools();
        try
        {
            Assert.True((await tools.PressAsync("Enter")).IsError != true);
            Assert.True((await tools.PressAsync("Ctrl+A")).IsError != true);
            Assert.True((await tools.KeyDownAsync("Shift")).IsError != true);
            Assert.True((await tools.KeyUpAsync("Shift")).IsError != true);
            Assert.True((await tools.TypeAsync("s1e2", "abc", delayMs: 5)).IsError != true);

            Assert.DoesNotContain(session.Calls, c => c.StartsWith("press:", StringComparison.Ordinal));
            Assert.DoesNotContain(session.Calls, c => c.StartsWith("key-down:", StringComparison.Ordinal));
            Assert.DoesNotContain(session.Calls, c => c.StartsWith("key-up:", StringComparison.Ordinal));
            Assert.Contains("type:s1e2:abc:5", session.Calls);

            Assert.Contains(keyboard.Calls, c => c.Contains("type:ENTER", StringComparison.Ordinal));
            Assert.Contains(keyboard.Calls, c => c.Contains("press:CONTROL", StringComparison.Ordinal));
            Assert.Contains(keyboard.Calls, c => c.Contains("type:KEY_A", StringComparison.Ordinal));
            Assert.Contains(keyboard.Calls, c => c.Contains("release:CONTROL", StringComparison.Ordinal));
            Assert.Contains(keyboard.Calls, c => c.Contains("press:SHIFT", StringComparison.Ordinal));
            Assert.Contains(keyboard.Calls, c => c.Contains("release:SHIFT", StringComparison.Ordinal));
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Mouse Actions Delegate To Session operation.</summary>
    [Fact]
    public async Task MouseActions_DelegateToSession()
    {
        var (tools, store, session, mouse, _) = CreateTools();
        try
        {
            Assert.True((await tools.DblclickAsync("s1e2", button: "middle", positionX: 7, positionY: 8)).IsError != true);
            Assert.True((await tools.HoverAsync("s1e2", positionX: 1, positionY: 2)).IsError != true);
            Assert.True((await tools.MouseMoveAsync("10,20")).IsError != true);
            Assert.True((await tools.MouseDownAsync(button: "right")).IsError != true);
            Assert.True((await tools.MouseUpAsync(button: "right")).IsError != true);
            Assert.True((await tools.MouseWheelAsync(deltaY: 3, deltaX: 4)).IsError != true);

            Assert.Contains("doubleclick:s1e2:Middle:7:8", session.Calls);
            Assert.Contains("hover:s1e2:1:2", session.Calls);
            Assert.DoesNotContain(session.Calls, c => c.StartsWith("mousemove:", StringComparison.Ordinal));
            Assert.DoesNotContain(session.Calls, c => c.StartsWith("mousedown:", StringComparison.Ordinal));
            Assert.DoesNotContain(session.Calls, c => c.StartsWith("mouseup:", StringComparison.Ordinal));
            Assert.DoesNotContain(session.Calls, c => c.StartsWith("mousewheel:", StringComparison.Ordinal));

            Assert.Contains("move:10,20", mouse.Calls);
            Assert.Contains("down:Right", mouse.Calls);
            Assert.Contains("up:Right", mouse.Calls);
            Assert.Contains("scroll:-3", mouse.Calls);
            Assert.Contains("hscroll:4", mouse.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Toggle And Focus Actions Delegate To Session operation.</summary>
    [Fact]
    public async Task ToggleAndFocusActions_DelegateToSession()
    {
        var (tools, store, session, _, _) = CreateTools();
        try
        {
            Assert.True((await tools.CheckAsync("s1e2")).IsError != true);
            Assert.True((await tools.UncheckAsync("s1e2")).IsError != true);
            Assert.True((await tools.SelectAsync("s1e2", name: ["Item"])).IsError != true);
            Assert.True((await tools.FocusAsync("s1e2")).IsError != true);
            Assert.True((await tools.ScrollIntoViewAsync("s1e2")).IsError != true);

            Assert.Contains("check:s1e2", session.Calls);
            Assert.Contains("uncheck:s1e2", session.Calls);
            Assert.Contains("select:s1e2:Item:Replace", session.Calls);
            Assert.Contains("focus:s1e2", session.Calls);
            Assert.Contains("scroll:s1e2", session.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Window Actions Delegate To Session operation.</summary>
    [Fact]
    public async Task WindowActions_DelegateToSession()
    {
        var (tools, store, session, _, _) = CreateTools();
        try
        {
            Assert.True((await tools.ResizeAsync(800, 600)).IsError != true);
            Assert.True((await tools.MinimizeAsync()).IsError != true);
            Assert.True((await tools.MaximizeAsync()).IsError != true);
            Assert.True((await tools.RestoreAsync()).IsError != true);

            Assert.Contains("resize:800:600", session.Calls);
            Assert.Contains("minimize", session.Calls);
            Assert.Contains("maximize", session.Calls);
            Assert.Contains("restore", session.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Lifecycle Actions Delegate And Detach On Success operation.</summary>
    [Theory]
    [InlineData("close")]
    [InlineData("kill")]
    public async Task LifecycleActions_DelegateAndDetachOnSuccess(string action)
    {
        var (tools, store, session, _, _) = CreateTools();
        try
        {
            var result = action == "close"
                ? await tools.CloseAsync()
                : await tools.KillAsync();

            Assert.True(result.IsError != true);
            Assert.Contains(action, session.Calls);
            Assert.True(session.Disposed);
            Assert.Empty(store.ListAll());
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Mouse Wheel Both Delta Zero Returns Invalid Argument operation.</summary>
    [Fact]
    public async Task MouseWheel_BothDeltaZero_ReturnsInvalidArgument()
    {
        var (tools, store, _, _, _) = CreateTools();
        try
        {
            var result = await tools.MouseWheelAsync(deltaY: 0, deltaX: 0);
            Assert.True(result.IsError == true);
            var doc = result.StructuredContent!.Value;
            Assert.Equal(ToolErrors.InvalidArgument, doc.GetProperty("code").GetString());
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Select Name And Index Returns Invalid Argument operation.</summary>
    [Fact]
    public async Task Select_NameAndIndex_ReturnsInvalidArgument()
    {
        var (tools, store, _, _, _) = CreateTools();
        try
        {
            var result = await tools.SelectAsync("s1e2", name: ["A"], index: [0]);
            Assert.True(result.IsError == true);
            var doc = result.StructuredContent!.Value;
            Assert.Equal(ToolErrors.InvalidArgument, doc.GetProperty("code").GetString());
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Select Add And Remove Returns Invalid Argument operation.</summary>
    [Fact]
    public async Task Select_AddAndRemove_ReturnsInvalidArgument()
    {
        var (tools, store, _, _, _) = CreateTools();
        try
        {
            var result = await tools.SelectAsync("s1e2", name: ["A"], add: true, remove: true);
            Assert.True(result.IsError == true);
            var doc = result.StructuredContent!.Value;
            Assert.Equal(ToolErrors.InvalidArgument, doc.GetProperty("code").GetString());
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Select Multiple Names Delegates To Session operation.</summary>
    [Fact]
    public async Task Select_MultipleNames_DelegatesToSession()
    {
        var (tools, store, session, _, _) = CreateTools();
        try
        {
            var result = await tools.SelectAsync("s1e2", name: ["A", "B"]);
            Assert.True(result.IsError != true);
            Assert.Contains("select:s1e2:A,B:Replace", session.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Select Add Mode Delegates To Session operation.</summary>
    [Fact]
    public async Task Select_AddMode_DelegatesToSession()
    {
        var (tools, store, session, _, _) = CreateTools();
        try
        {
            var result = await tools.SelectAsync("s1e2", name: ["C"], add: true);
            Assert.True(result.IsError != true);
            Assert.Contains("select:s1e2:C:Add", session.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Select No Selectors Returns Invalid Argument operation.</summary>
    [Fact]
    public async Task Select_NoSelectors_ReturnsInvalidArgument()
    {
        var (tools, store, _, _, _) = CreateTools();
        try
        {
            var result = await tools.SelectAsync("s1e2");
            Assert.True(result.IsError == true);
            var doc = result.StructuredContent!.Value;
            Assert.Equal(ToolErrors.InvalidArgument, doc.GetProperty("code").GetString());
        }
        finally { store.Dispose(); }
    }
}
