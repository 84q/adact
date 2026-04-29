using System.Text.Json;

using Adact.Engine;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>
/// WindowsTools のライフサイクル系メソッド (detach / close / kill / close_all / daemon_stop) を
/// Engine 操作なしで検証する Unit テスト。成功パス (Engine 操作伴う) は L3 IntegrationUia /
/// L4 Smoke (#9) で別途検証する。
/// </summary>
[Trait("Layer", "Unit")]
public class WindowsToolsLifecycleTests
{
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
