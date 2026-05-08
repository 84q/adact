using System.Text.Json;

using Adact.Engine;
using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>Contains tests for the Windows Tools Lifecycle behavior.</summary>
[Trait("Layer", "Unit")]
public class WindowsToolsLifecycleTests
{
    private sealed class FakeWindowSession : IWindowSession
    {
        /// <summary>Gets the Session Id value.</summary>
        public int SessionId { get; init; }
        /// <summary>Gets the Process Name value.</summary>
        public string ProcessName { get; init; } = "fake";
        /// <summary>Gets the Process Id value.</summary>
        public int ProcessId { get; init; }
        /// <summary>Gets the Title value.</summary>
        public string Title { get; init; } = "Fake";
        /// <summary>Gets the Native Window Handle value.</summary>
        public nint NativeWindowHandle { get; init; }
        /// <summary>Gets the On Close Async value.</summary>
        public Func<CancellationToken, Task>? OnCloseAsync { get; init; }

        /// <summary>Performs the Snapshot Async operation.</summary>
        public Task<SnapshotResult> SnapshotAsync(SnapshotOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Click Async operation.</summary>
        public Task ClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Click With Options Async operation.</summary>
        public Task ClickWithOptionsAsync(string refId, ClickOptions options, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Double Click Async operation.</summary>
        public Task DoubleClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Fill Async operation.</summary>
        public Task FillAsync(string refId, string text, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Press Async operation.</summary>
        public Task PressAsync(string key, string? refId = null, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Key Down Async operation.</summary>
        public Task KeyDownAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Key Up Async operation.</summary>
        public Task KeyUpAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Type Async operation.</summary>
        public Task TypeAsync(string? refId, string text, int delayMs = 0, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Hover Async operation.</summary>
        public Task HoverAsync(string refId, IReadOnlyList<string>? modifiers = null, int? positionX = null, int? positionY = null, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Mouse Move Async operation.</summary>
        public Task MouseMoveAsync(MouseTarget target, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Mouse Down Async operation.</summary>
        public Task MouseDownAsync(MouseTarget target, MouseButton button, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Mouse Up Async operation.</summary>
        public Task MouseUpAsync(MouseTarget target, MouseButton button, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Mouse Wheel Async operation.</summary>
        public Task MouseWheelAsync(MouseTarget target, int deltaX, int deltaY, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Check Async operation.</summary>
        public Task CheckAsync(string refId, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Uncheck Async operation.</summary>
        public Task UncheckAsync(string refId, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Select Async operation.</summary>
        public Task SelectAsync(string refId, SelectionTarget[] targets, SelectionMode mode = SelectionMode.Replace, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Focus Async operation.</summary>
        public Task FocusAsync(string refId, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Scroll Into View Async operation.</summary>
        public Task ScrollIntoViewAsync(string refId, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Scroll Async operation.</summary>
        public Task ScrollAsync(string refId, ScrollMode mode, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Inspect Async operation.</summary>
        public Task<InspectResult> InspectAsync(string refId, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Screenshot Async operation.</summary>
        public Task<ScreenshotResult> ScreenshotAsync(string? refId, string? outPath, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Resize Async operation.</summary>
        public Task ResizeAsync(int? width, int? height, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Minimize Async operation.</summary>
        public Task MinimizeAsync(CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Maximize Async operation.</summary>
        public Task MaximizeAsync(CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Restore Async operation.</summary>
        public Task RestoreAsync(CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Waits for the Wait For Ref Async condition.</summary>
        public Task<WaitForResult> WaitForRefAsync(string refId, WaitForState state, TimeSpan timeout, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Waits for the Wait For Query Async condition.</summary>
        public Task<WaitForResult> WaitForQueryAsync(WaitForElementQuery query, WaitForState state, TimeSpan timeout, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Performs the Close Async operation.</summary>
        public Task CloseAsync(CancellationToken ct = default) => OnCloseAsync?.Invoke(ct) ?? Task.CompletedTask;
        /// <summary>Performs the Kill Async operation.</summary>
        public Task<KillMethod> KillAsync(bool force = false, int timeoutMs = 5000, CancellationToken ct = default) => throw new NotSupportedException();
        /// <summary>Releases resources.</summary>
        public void Dispose() { }
    }

    private sealed class FakeDaemonControl : IDaemonControl
    {
        /// <summary>Gets a value indicating whether Is Supported.</summary>
        public bool IsSupported { get; init; }
        /// <summary>Gets or sets the Stop Call Count value.</summary>
        public int StopCallCount { get; private set; }
        /// <summary>Gets the On Stop value.</summary>
        public Action<int>? OnStop { get; init; }
        /// <summary>Performs the Stop Async operation.</summary>
        public Task StopAsync(CancellationToken ct)
        {
            StopCallCount++;
            OnStop?.Invoke(StopCallCount);
            return Task.CompletedTask;
        }
    }

    private static (WindowsTools tools, SessionStore store, WindowRefStore refStore, FakeDaemonControl daemon)
        CreateTools(bool daemonSupported = true)
    {
        var engine = new UiaEngine();
        var store = new SessionStore(engine);
        var refStore = new WindowRefStore();
        var daemon = new FakeDaemonControl { IsSupported = daemonSupported };
        var tools = new WindowsTools(store, refStore, daemon);
        return (tools, store, refStore, daemon);
    }

    private static (string code, string message) ReadError(CallToolResult result)
    {
        Assert.True(result.IsError == true, "Expected IsError=true");
        Assert.NotNull(result.StructuredContent);
        var doc = result.StructuredContent.Value;
        return (
            doc.GetProperty("code").GetString()!,
            doc.GetProperty("message").GetString()!);
    }

    /// <summary>Performs the Detach No Session Id And No Active Returns No Active Session operation.</summary>
    [Fact]
    public async Task Detach_NoSessionIdAndNoActive_ReturnsNoActiveSession()
    {
        var (tools, store, _, _) = CreateTools();
        try
        {
            var result = await tools.DetachAsync(sessionId: null);
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NoActiveSession, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Detach Unknown Session Id Returns Not Found operation.</summary>
    [Fact]
    public async Task Detach_UnknownSessionId_ReturnsNotFound()
    {
        var (tools, store, _, _) = CreateTools();
        try
        {
            var result = await tools.DetachAsync(sessionId: "s99");
            var (code, msg) = ReadError(result);
            Assert.Equal(ToolErrors.NotFound, code);
            Assert.Contains("s99", msg);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Close No Active Session Returns No Active Session operation.</summary>
    [Fact]
    public async Task Close_NoActiveSession_ReturnsNoActiveSession()
    {
        var (tools, store, _, _) = CreateTools();
        try
        {
            var result = await tools.CloseAsync(sessionId: null);
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NoActiveSession, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Close Unknown Session Id Returns Not Found operation.</summary>
    [Fact]
    public async Task Close_UnknownSessionId_ReturnsNotFound()
    {
        var (tools, store, _, _) = CreateTools();
        try
        {
            var result = await tools.CloseAsync(sessionId: "s42");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NotFound, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Kill Unknown Session Id Returns Not Found operation.</summary>
    [Fact]
    public async Task Kill_UnknownSessionId_ReturnsNotFound()
    {
        var (tools, store, _, _) = CreateTools();
        try
        {
            var result = await tools.KillAsync(sessionId: "s42");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NotFound, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Detach Removes Associated Window Ref Entry operation.</summary>
    [Fact]
    public async Task Detach_RemovesAssociatedWindowRefEntry()
    {
        var (tools, store, refStore, _) = CreateTools();
        try
        {
            var session = new FakeWindowSession { SessionId = 1, ProcessId = 100, NativeWindowHandle = 0x1000 };
            store.Register(session);
            var entry = refStore.SyncOrAssign(new WindowKey(0x1000, 100, DateTime.MinValue), new WindowInfo(100, "fake", "Fake", "Window", null, 0x1000));
            refStore.AssociateSession(entry.WindowRef, "s1");

            var result = await tools.DetachAsync("s1");

            Assert.False(result.IsError ?? false);
            Assert.False(refStore.TryFindByKey(new WindowKey(0x1000, 100, DateTime.MinValue), out _));
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Daemon Stop Stdio Mode Returns Local Only operation.</summary>
    [Fact]
    public async Task DaemonStop_StdioMode_ReturnsLocalOnly()
    {
        var (tools, store, _, daemon) = CreateTools(daemonSupported: false);
        try
        {
            var result = await tools.DaemonStopAsync();
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.LocalOnly, code);
            Assert.Equal(0, daemon.StopCallCount);
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Daemon Stop Http Mode Invokes Control Stop operation.</summary>
    [Fact]
    public async Task DaemonStop_HttpMode_InvokesControlStop()
    {
        var (tools, store, _, daemon) = CreateTools(daemonSupported: true);
        try
        {
            var result = await tools.DaemonStopAsync();
            Assert.True(result.IsError != true);
            Assert.Equal(1, daemon.StopCallCount);
            Assert.NotNull(result.StructuredContent);
            Assert.True(result.StructuredContent.Value.GetProperty("stopped").GetBoolean());
        }
        finally { store.Dispose(); }
    }

    /// <summary>Performs the Daemon Stop Detaches All Sessions Before Stopping Listener operation.</summary>
    [Fact]
    public async Task DaemonStop_DetachesAllSessionsBeforeStoppingListener()
    {
        var engine = new UiaEngine();
        var store = new SessionStore(engine);
        var refStore = new WindowRefStore();

        bool sessionsEmptyAtStop = false;
        bool refStoreSessionsCleared = false;

        var daemon = new FakeDaemonControl
        {
            IsSupported = true,
            OnStop = _ =>
            {
                sessionsEmptyAtStop = store.ListAll().Count == 0;
                refStoreSessionsCleared = refStore.ListActive()
                    .All(e => e.SessionId is null);
            },
        };
        var tools = new WindowsTools(store, refStore, daemon);

        try
        {
            var info1 = new WindowInfo(
                ProcessId: 10001, ProcessName: "fake1", Title: "Fake 1",
                ControlType: "Window", ClassName: null, NativeWindowHandle: 0x1001);
            var info2 = new WindowInfo(
                ProcessId: 10002, ProcessName: "fake2", Title: "Fake 2",
                ControlType: "Window", ClassName: null, NativeWindowHandle: 0x1002);

            var session1 = WindowSession.CreateForTest(1, info1);
            var session2 = WindowSession.CreateForTest(2, info2);
            store.Register(session1);
            store.Register(session2);

            var entry1 = refStore.SyncOrAssign(WindowKey.From(info1), info1);
            var entry2 = refStore.SyncOrAssign(WindowKey.From(info2), info2);
            refStore.AssociateSession(entry1.WindowRef, "s1");
            refStore.AssociateSession(entry2.WindowRef, "s2");

            var result = await tools.DaemonStopAsync();

            Assert.True(result.IsError != true);
            Assert.Equal(1, daemon.StopCallCount);
            Assert.True(sessionsEmptyAtStop, "sessions should be empty before StopAsync runs");
            Assert.True(refStoreSessionsCleared,
                "WindowRefStore entries should have null SessionId before StopAsync runs");
            Assert.Empty(store.ListAll());
            Assert.All(refStore.ListActive(), e => Assert.Null(e.SessionId));
        }
        finally { store.Dispose(); }
    }
}
