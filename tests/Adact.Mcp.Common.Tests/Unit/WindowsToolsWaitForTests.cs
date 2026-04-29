using Adact.Engine;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>
/// <see cref="WindowsTools.WaitForAsync"/> および <see cref="WindowsTools.WaitForWindowAsync"/>
/// (Phase 8 Step 7) の引数検証 / セッション解決エラーを検証する Unit テスト。
/// 実 UIA を呼ばない範囲のみを対象とし、ポーリング成功パスは Integration / Smoke で別途検証する。
/// </summary>
[Trait("Layer", "Unit")]
public class WindowsToolsWaitForTests
{
    private sealed class FakeDaemonControl : IDaemonControl
    {
        public bool IsSupported { get; init; } = true;
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private static (WindowsTools tools, SessionStore store) CreateTools()
    {
        var engine = new UiaEngine();
        var store = new SessionStore(engine);
        var refStore = new WindowRefStore();
        var daemon = new FakeDaemonControl();
        var tools = new WindowsTools(store, refStore, daemon);
        return (tools, store);
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

    /// <summary>windows_wait_for: ref と search 条件の同時指定は INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitFor_RefAndQueryBoth_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForAsync(@ref: "s1e1", name: "OK");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for: ref も検索条件も無い場合 INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitFor_NoConditions_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForAsync();
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for: 未知の state 値は INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitFor_UnknownState_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForAsync(@ref: "s1e1", state: "focused");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for: timeoutMs ≤ 0 は INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitFor_NonPositiveTimeout_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForAsync(@ref: "s1e1", timeoutMs: 0);
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for: timeoutMs 負値も INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitFor_NegativeTimeout_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForAsync(@ref: "s1e1", timeoutMs: -1);
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for: ref と sessionId を同時指定すると INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitFor_RefWithSessionId_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForAsync(@ref: "s1e1", sessionId: "s1");
            var (code, message) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
            Assert.Contains("sessionId", message, StringComparison.OrdinalIgnoreCase);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for: 形式不正な ref は REF_NOT_FOUND。</summary>
    [Fact]
    public async Task WaitFor_MalformedRef_ReturnsRefNotFound()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForAsync(@ref: "not-a-ref");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.RefNotFound, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for: 未登録 session の ref は REF_NOT_FOUND。</summary>
    [Fact]
    public async Task WaitFor_UnknownSessionRef_ReturnsRefNotFound()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForAsync(@ref: "s99e1");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.RefNotFound, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for: 検索条件モードで active session 無しなら NO_ACTIVE_SESSION。</summary>
    [Fact]
    public async Task WaitFor_QueryMode_NoActiveSession_ReturnsNoActiveSession()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForAsync(name: "OK");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NoActiveSession, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for: 検索条件モードで未登録 sessionId は NOT_FOUND。</summary>
    [Fact]
    public async Task WaitFor_QueryMode_UnknownSessionId_ReturnsNotFound()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForAsync(name: "OK", sessionId: "s99");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NotFound, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for_window: 条件未指定は INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitForWindow_NoConditions_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForWindowAsync();
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for_window: timeoutMs ≤ 0 は INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitForWindow_NonPositiveTimeout_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForWindowAsync(title: "anything", timeoutMs: 0);
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>windows_wait_for_window: timeoutMs 負値も INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitForWindow_NegativeTimeout_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.WaitForWindowAsync(title: "anything", timeoutMs: -10);
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>ToolErrors.WaitTimeout 定数の値検証 (回帰防止)。</summary>
    [Fact]
    public void ToolErrors_WaitTimeout_HasExpectedWireValue()
    {
        Assert.Equal("WAIT_TIMEOUT", ToolErrors.WaitTimeout);
    }

    /// <summary>WaitTimeoutException は ToolErrors.WaitTimeout にマップされる。</summary>
    [Fact]
    public void ToolErrors_TryMap_MapsWaitTimeoutException()
    {
        var ex = new Adact.Engine.Exceptions.WaitTimeoutException("timed out");
        var mapped = ToolErrors.TryMap(ex);
        Assert.NotNull(mapped);
        Assert.True(mapped!.IsError == true);
        Assert.Equal(ToolErrors.WaitTimeout,
            mapped.StructuredContent!.Value.GetProperty("code").GetString());
    }
}
