using System.Text.Json;

using Adact.Engine;
using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>
/// WindowsTools のライフサイクル系メソッド (detach / close / kill / close_all / adact_daemon_stop) を
/// Engine 操作なしで検証する Unit テスト。成功パス (Engine 操作伴う) は L3 IntegrationUia /
/// L4 Smoke (#9) で別途検証する。
/// </summary>
[Trait("Layer", "Unit")]
public class WindowsToolsLifecycleTests
{
    private sealed class FakeWindowSession : IWindowSession
    {
        public int SessionId { get; init; }
        public string ProcessName { get; init; } = "fake";
        public int ProcessId { get; init; }
        public string Title { get; init; } = "Fake";
        public nint NativeWindowHandle { get; init; }
        public Func<CancellationToken, Task>? OnCloseAsync { get; init; }

        public Task<SnapshotResult> SnapshotAsync(SnapshotOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ClickWithOptionsAsync(string refId, ClickOptions options, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DoubleClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task FillAsync(string refId, string text, CancellationToken ct = default) => throw new NotSupportedException();
        public Task PressAsync(string key, string? refId = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task KeyDownAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();
        public Task KeyUpAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();
        public Task TypeAsync(string? refId, string text, int delayMs = 0, CancellationToken ct = default) => throw new NotSupportedException();
        public Task HoverAsync(string refId, IReadOnlyList<string>? modifiers = null, int? positionX = null, int? positionY = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task MouseMoveAsync(MouseTarget target, CancellationToken ct = default) => throw new NotSupportedException();
        public Task MouseDownAsync(MouseTarget target, MouseButton button, CancellationToken ct = default) => throw new NotSupportedException();
        public Task MouseUpAsync(MouseTarget target, MouseButton button, CancellationToken ct = default) => throw new NotSupportedException();
        public Task MouseWheelAsync(MouseTarget target, int deltaX, int deltaY, CancellationToken ct = default) => throw new NotSupportedException();
        public Task CheckAsync(string refId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UncheckAsync(string refId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SelectAsync(string refId, string? name, int? index, string? itemRef, CancellationToken ct = default) => throw new NotSupportedException();
        public Task FocusAsync(string refId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ClearAsync(string refId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ScrollIntoViewAsync(string refId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<InspectResult> InspectAsync(string refId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ScreenshotResult> ScreenshotAsync(string? refId, string? outPath, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ResizeAsync(int width, int height, CancellationToken ct = default) => throw new NotSupportedException();
        public Task MinimizeAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task MaximizeAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task RestoreAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<WaitForResult> WaitForRefAsync(string refId, WaitForState state, TimeSpan timeout, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<WaitForResult> WaitForQueryAsync(WaitForElementQuery query, WaitForState state, TimeSpan timeout, CancellationToken ct = default) => throw new NotSupportedException();
        public Task CloseAsync(CancellationToken ct = default) => OnCloseAsync?.Invoke(ct) ?? Task.CompletedTask;
        public Task KillAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class FakeDaemonControl : IDaemonControl
    {
        public bool IsSupported { get; init; }
        public int StopCallCount { get; private set; }
        public Action<int>? OnStop { get; init; }
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

    /// <summary>
    /// sessionId 未指定かつアクティブセッションも無い状態で detach を呼ぶと NoActiveSession エラーになることを確認する。
    /// 暗黙のアクティブセッション解決が失敗した時のエラーコード仕様の回帰防止。
    /// </summary>
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

    /// <summary>
    /// 未登録の sessionId を指定して detach すると NotFound エラーとメッセージ中の sessionId が返ることを確認する。
    /// 誤った sessionId に対するエラー応答契約の回帰防止。
    /// </summary>
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

    /// <summary>
    /// アクティブセッションが無い状態で close を呼ぶと NoActiveSession エラーになることを確認する。
    /// detach 系と同じ暗黙解決失敗エラー仕様の回帰防止。
    /// </summary>
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

    /// <summary>
    /// 未登録の sessionId で close を呼ぶと NotFound エラーになることを確認する。
    /// </summary>
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

    /// <summary>
    /// 未登録の sessionId で kill を呼ぶと NotFound エラーになることを確認する。
    /// </summary>
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

    /// <summary>
    /// セッションが 1 つも無い状態で close_all を呼ぶと、エラー無しで空 results が返ることを確認する。
    /// 「セッション無し」が異常終了ではなく正常応答として扱われる契約の回帰防止。
    /// </summary>
    [Fact]
    public async Task CloseAll_EmptyStore_ReturnsEmptyResults()
    {
        var (tools, store, _, _) = CreateTools();
        try
        {
            var result = await tools.CloseAllAsync();
            Assert.True(result.IsError != true);
            Assert.NotNull(result.StructuredContent);
            var arr = result.StructuredContent.Value.GetProperty("results");
            Assert.Equal(JsonValueKind.Array, arr.ValueKind);
            Assert.Equal(0, arr.GetArrayLength());
        }
        finally { store.Dispose(); }
    }

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

    /// <summary>
    /// close_all は CloseFailedException を個別結果へ変換しつつ残り session を継続し、hasFailures=true を返す。
    /// </summary>
    [Fact]
    public async Task CloseAll_CloseFailedException_ContinuesAndReturnsFailureEntry()
    {
        var (tools, store, _, _) = CreateTools();
        try
        {
            store.Register(new FakeWindowSession { SessionId = 1, OnCloseAsync = _ => Task.CompletedTask });
            store.Register(new FakeWindowSession { SessionId = 2, OnCloseAsync = _ => Task.FromException(new CloseFailedException("close failed")) });

            var result = await tools.CloseAllAsync();

            Assert.True(result.IsError != true);
            var payload = result.StructuredContent!.Value;
            Assert.True(payload.GetProperty("hasFailures").GetBoolean());
            var entries = payload.GetProperty("results").EnumerateArray().ToArray();
            Assert.Contains(entries, e => e.GetProperty("sessionId").GetString() == "s1" && e.GetProperty("result").GetString() == "ok");
            Assert.Contains(entries, e => e.GetProperty("sessionId").GetString() == "s2"
                && e.GetProperty("result").GetString() == "fail"
                && e.GetProperty("error").GetString() == ToolErrors.CloseFailed);
            Assert.False(store.TryGet("s1", out _));
            Assert.True(store.TryGet("s2", out _));
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// close_all は想定外例外も INTERNAL_ERROR として session 単位で結果化し、残り session の close を継続する。
    /// </summary>
    [Fact]
    public async Task CloseAll_UnexpectedException_ContinuesAndReturnsInternalErrorEntry()
    {
        var (tools, store, _, _) = CreateTools();
        try
        {
            store.Register(new FakeWindowSession { SessionId = 1, OnCloseAsync = _ => Task.FromException(new InvalidOperationException("boom")) });
            store.Register(new FakeWindowSession { SessionId = 2, OnCloseAsync = _ => Task.CompletedTask });

            var result = await tools.CloseAllAsync();

            Assert.True(result.IsError != true);
            var payload = result.StructuredContent!.Value;
            Assert.True(payload.GetProperty("hasFailures").GetBoolean());
            var entries = payload.GetProperty("results").EnumerateArray().ToArray();
            Assert.Contains(entries, e => e.GetProperty("sessionId").GetString() == "s1"
                && e.GetProperty("result").GetString() == "fail"
                && e.GetProperty("error").GetString() == ToolErrors.InternalError
                && e.GetProperty("message").GetString() == "boom");
            Assert.Contains(entries, e => e.GetProperty("sessionId").GetString() == "s2" && e.GetProperty("result").GetString() == "ok");
            Assert.True(store.TryGet("s1", out _));
            Assert.False(store.TryGet("s2", out _));
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// close_all 中のキャンセルは握りつぶさず伝播する。
    /// </summary>
    [Fact]
    public async Task CloseAll_Cancellation_Propagates()
    {
        var (tools, store, _, _) = CreateTools();
        try
        {
            store.Register(new FakeWindowSession { SessionId = 1, OnCloseAsync = ct => Task.FromCanceled(ct) });

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tools.CloseAllAsync(cts.Token));
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// IDaemonControl が未対応 (stdio モード相当) の場合、adact_daemon_stop が LocalOnly エラーを返し StopAsync を呼ばないことを確認する。
    /// stdio 経由では HTTP daemon を停止できない仕様の回帰防止。
    /// </summary>
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

    /// <summary>
    /// HTTP モードで adact_daemon_stop を呼ぶと IDaemonControl.StopAsync が 1 回起動され、stopped=true が返ることを確認する。
    /// HTTP 経由の daemon 停止フロー (Phase5) の回帰防止。
    /// </summary>
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

    /// <summary>
    /// adact_daemon_stop が StopAsync を呼ぶ時点までに全セッションが detach され、WindowRefStore の SessionId も全クリアされていることを確認する。
    /// listener 停止前にセッションを掃除する順序契約 (Phase5 §adact_daemon_stop) の回帰防止。
    /// </summary>
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
            // 2 つの dummy session を SessionStore へ流し込み、対応する WindowRefStore エントリを
            // SessionId 紐付け済の状態で作る。
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
            // detach は StopAsync の前に完了していること。
            Assert.True(sessionsEmptyAtStop, "sessions should be empty before StopAsync runs");
            Assert.True(refStoreSessionsCleared,
                "WindowRefStore entries should have null SessionId before StopAsync runs");
            // 事後状態の確認も。
            Assert.Empty(store.ListAll());
            Assert.All(refStore.ListActive(), e => Assert.Null(e.SessionId));
        }
        finally { store.Dispose(); }
    }
}
