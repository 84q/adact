using Adact.Engine;
using Adact.Engine.Snapshot;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>
/// Verifies WindowsTools action tools delegate successful calls to IWindowSession without UIA.
/// </summary>
[Trait("Layer", "Unit")]
public class WindowsToolsActionSuccessTests
{
    private sealed class FakeDaemonControl : IDaemonControl
    {
        public bool IsSupported => true;
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeWindowSession : IWindowSession
    {
        public List<string> Calls { get; } = [];
        public int SessionId { get; init; } = 1;
        public string ProcessName { get; init; } = "fake";
        public int ProcessId { get; init; } = 1234;
        public string Title { get; init; } = "Fake";
        public nint NativeWindowHandle { get; init; } = 0x1234;
        public bool Disposed { get; private set; }

        public Task<SnapshotResult> SnapshotAsync(SnapshotOptions? options = null, CancellationToken ct = default)
            => Task.FromResult(new SnapshotResult("{}", "s1", Title, ProcessName, ProcessId, DateTimeOffset.UtcNow));

        public Task ClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default)
        {
            Calls.Add($"click:{refId}:{(options is null ? "null" : "options")}");
            return Task.CompletedTask;
        }

        public Task ClickWithOptionsAsync(string refId, ClickOptions options, CancellationToken ct = default)
        {
            Calls.Add($"click-options:{refId}:{options.Button}:{options.Count}:{options.PositionX}:{options.PositionY}");
            return Task.CompletedTask;
        }

        public Task DoubleClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default)
        {
            Calls.Add($"dblclick:{refId}:{options?.Button}:{options?.PositionX}:{options?.PositionY}");
            return Task.CompletedTask;
        }

        public Task FillAsync(string refId, string text, CancellationToken ct = default)
        {
            Calls.Add($"fill:{refId}:{text}");
            return Task.CompletedTask;
        }

        public Task PressAsync(string key, string? refId = null, CancellationToken ct = default)
        {
            Calls.Add($"press:{key}:{refId ?? "<window>"}");
            return Task.CompletedTask;
        }

        public Task KeyDownAsync(string key, CancellationToken ct = default)
        {
            Calls.Add($"key-down:{key}");
            return Task.CompletedTask;
        }

        public Task KeyUpAsync(string key, CancellationToken ct = default)
        {
            Calls.Add($"key-up:{key}");
            return Task.CompletedTask;
        }

        public Task TypeAsync(string? refId, string text, int delayMs = 0, CancellationToken ct = default)
        {
            Calls.Add($"type:{refId}:{text}:{delayMs}");
            return Task.CompletedTask;
        }

        public Task HoverAsync(string refId, IReadOnlyList<string>? modifiers = null, int? positionX = null, int? positionY = null, CancellationToken ct = default)
        {
            Calls.Add($"hover:{refId}:{positionX}:{positionY}");
            return Task.CompletedTask;
        }

        public Task MouseMoveAsync(MouseTarget target, CancellationToken ct = default)
        {
            Calls.Add($"mouse-move:{Describe(target)}");
            return Task.CompletedTask;
        }

        public Task MouseDownAsync(MouseTarget target, MouseButton button, CancellationToken ct = default)
        {
            Calls.Add($"mouse-down:{Describe(target)}:{button}");
            return Task.CompletedTask;
        }

        public Task MouseUpAsync(MouseTarget target, MouseButton button, CancellationToken ct = default)
        {
            Calls.Add($"mouse-up:{Describe(target)}:{button}");
            return Task.CompletedTask;
        }

        public Task MouseWheelAsync(MouseTarget target, int deltaX, int deltaY, CancellationToken ct = default)
        {
            Calls.Add($"mouse-wheel:{Describe(target)}:{deltaX}:{deltaY}");
            return Task.CompletedTask;
        }

        public Task CheckAsync(string refId, CancellationToken ct = default)
        {
            Calls.Add($"check:{refId}");
            return Task.CompletedTask;
        }

        public Task UncheckAsync(string refId, CancellationToken ct = default)
        {
            Calls.Add($"uncheck:{refId}");
            return Task.CompletedTask;
        }

        public Task SelectAsync(string refId, string? name, int? index, string? itemRef, CancellationToken ct = default)
        {
            Calls.Add($"select:{refId}:{name}:{index}:{itemRef}");
            return Task.CompletedTask;
        }

        public Task FocusAsync(string refId, CancellationToken ct = default)
        {
            Calls.Add($"focus:{refId}");
            return Task.CompletedTask;
        }

        public Task ClearAsync(string refId, CancellationToken ct = default)
        {
            Calls.Add($"clear:{refId}");
            return Task.CompletedTask;
        }

        public Task ScrollIntoViewAsync(string refId, CancellationToken ct = default)
        {
            Calls.Add($"scroll:{refId}");
            return Task.CompletedTask;
        }

        public Task<InspectResult> InspectAsync(string refId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<ScreenshotResult> ScreenshotAsync(string? refId, string? outPath, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task ResizeAsync(int width, int height, CancellationToken ct = default)
        {
            Calls.Add($"resize:{width}:{height}");
            return Task.CompletedTask;
        }

        public Task MinimizeAsync(CancellationToken ct = default)
        {
            Calls.Add("minimize");
            return Task.CompletedTask;
        }

        public Task MaximizeAsync(CancellationToken ct = default)
        {
            Calls.Add("maximize");
            return Task.CompletedTask;
        }

        public Task RestoreAsync(CancellationToken ct = default)
        {
            Calls.Add("restore");
            return Task.CompletedTask;
        }

        public Task<WaitForResult> WaitForRefAsync(string refId, WaitForState state, TimeSpan timeout, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<WaitForResult> WaitForQueryAsync(WaitForElementQuery query, WaitForState state, TimeSpan timeout, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task CloseAsync(CancellationToken ct = default)
        {
            Calls.Add("close");
            return Task.CompletedTask;
        }

        public Task KillAsync(CancellationToken ct = default)
        {
            Calls.Add("kill");
            return Task.CompletedTask;
        }

        public void Dispose() => Disposed = true;

        private static string Describe(MouseTarget target)
            => target switch
            {
                MouseTarget.ByRef r => r.Ref,
                MouseTarget.ByPoint p => $"{p.X},{p.Y}",
                _ => target.ToString() ?? "<unknown>",
            };
    }

    private static (WindowsTools Tools, SessionStore Store, FakeWindowSession Session) CreateTools()
    {
        var store = new SessionStore(new UiaEngine());
        var session = new FakeWindowSession();
        store.Register(session);
        var tools = new WindowsTools(store, new WindowRefStore(), new FakeDaemonControl());
        return (tools, store, session);
    }

    /// <summary>
    /// Click without extensions delegates to ClickAsync.
    /// </summary>
    [Fact]
    public async Task Click_Default_DelegatesToSessionClick()
    {
        var (tools, store, session) = CreateTools();
        try
        {
            var result = await tools.ClickAsync("s1e2");

            Assert.True(result.IsError != true);
            Assert.Contains("click:s1e2:null", session.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// Click with extensions delegates to ClickWithOptionsAsync.
    /// </summary>
    [Fact]
    public async Task Click_WithOptions_DelegatesToSessionClickWithOptions()
    {
        var (tools, store, session) = CreateTools();
        try
        {
            var result = await tools.ClickAsync("s1e2", button: "right", count: 2, positionX: 3, positionY: 4);

            Assert.True(result.IsError != true);
            Assert.Contains("click-options:s1e2:Right:2:3:4", session.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// Fill delegates to IWindowSession.
    /// </summary>
    [Fact]
    public async Task Fill_DelegatesToSession()
    {
        var (tools, store, session) = CreateTools();
        try
        {
            var result = await tools.FillAsync("s1e2", "value");

            Assert.True(result.IsError != true);
            Assert.Contains("fill:s1e2:value", session.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// Keyboard action tools delegate to IWindowSession.
    /// </summary>
    [Fact]
    public async Task KeyboardActions_DelegateToSession()
    {
        var (tools, store, session) = CreateTools();
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
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// Mouse action tools delegate to IWindowSession with parsed targets.
    /// </summary>
    [Fact]
    public async Task MouseActions_DelegateToSession()
    {
        var (tools, store, session) = CreateTools();
        try
        {
            Assert.True((await tools.DblclickAsync("s1e2", button: "middle", positionX: 7, positionY: 8)).IsError != true);
            Assert.True((await tools.HoverAsync("s1e2", positionX: 1, positionY: 2)).IsError != true);
            Assert.True((await tools.MouseMoveAsync("10,20")).IsError != true);
            Assert.True((await tools.MouseDownAsync(button: "right")).IsError != true);
            Assert.True((await tools.MouseUpAsync(button: "right")).IsError != true);
            Assert.True((await tools.MouseWheelAsync(deltaY: 3, deltaX: 4)).IsError != true);

            Assert.Contains("dblclick:s1e2:Middle:7:8", session.Calls);
            Assert.Contains("hover:s1e2:1:2", session.Calls);
            Assert.DoesNotContain(session.Calls, c => c.StartsWith("mouse-move:", StringComparison.Ordinal));
            Assert.DoesNotContain(session.Calls, c => c.StartsWith("mouse-down:", StringComparison.Ordinal));
            Assert.DoesNotContain(session.Calls, c => c.StartsWith("mouse-up:", StringComparison.Ordinal));
            Assert.DoesNotContain(session.Calls, c => c.StartsWith("mouse-wheel:", StringComparison.Ordinal));
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// Toggle and focus action tools delegate to IWindowSession.
    /// </summary>
    [Fact]
    public async Task ToggleAndFocusActions_DelegateToSession()
    {
        var (tools, store, session) = CreateTools();
        try
        {
            Assert.True((await tools.CheckAsync("s1e2")).IsError != true);
            Assert.True((await tools.UncheckAsync("s1e2")).IsError != true);
            Assert.True((await tools.SelectAsync("s1e2", name: "Item")).IsError != true);
            Assert.True((await tools.FocusAsync("s1e2")).IsError != true);
            Assert.True((await tools.ClearAsync("s1e2")).IsError != true);
            Assert.True((await tools.ScrollIntoViewAsync("s1e2")).IsError != true);

            Assert.Contains("check:s1e2", session.Calls);
            Assert.Contains("uncheck:s1e2", session.Calls);
            Assert.Contains("select:s1e2:Item::", session.Calls);
            Assert.Contains("focus:s1e2", session.Calls);
            Assert.Contains("clear:s1e2", session.Calls);
            Assert.Contains("scroll:s1e2", session.Calls);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// Window state tools delegate to IWindowSession.
    /// </summary>
    [Fact]
    public async Task WindowActions_DelegateToSession()
    {
        var (tools, store, session) = CreateTools();
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

    /// <summary>
    /// Close and kill delegate to IWindowSession and detach the session on success.
    /// </summary>
    [Theory]
    [InlineData("close")]
    [InlineData("kill")]
    public async Task LifecycleActions_DelegateAndDetachOnSuccess(string action)
    {
        var (tools, store, session) = CreateTools();
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
}
