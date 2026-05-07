using Adact.Engine;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>
/// <see cref="WindowsTools.InspectAsync"/> および <see cref="WindowsTools.ScreenshotAsync"/>
/// (Phase 8 Step 6) の引数検証 / セッション解決エラーを検証する Unit テスト。
/// 実 UIA を呼ばない範囲のみを対象とし、成功パスは Integration / Smoke で別途検証する。
/// </summary>
[Trait("Layer", "Unit")]
public class WindowsToolsInspectScreenshotTests
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

    /// <summary>adact_inspect: 空 ref は INVALID_ARGUMENT を返す。</summary>
    [Fact]
    public async Task Inspect_EmptyRef_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.InspectAsync(@ref: string.Empty);
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>adact_inspect: 形式不正な ref は REF_NOT_FOUND を返す。</summary>
    [Fact]
    public async Task Inspect_MalformedRef_ReturnsRefNotFound()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.InspectAsync(@ref: "not-a-ref");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.RefNotFound, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>adact_inspect: 未登録 session の ref は REF_NOT_FOUND を返す。</summary>
    [Fact]
    public async Task Inspect_UnknownSession_ReturnsRefNotFound()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.InspectAsync(@ref: "s99e1");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.RefNotFound, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>adact_screenshot: ref / sessionId 未指定で active session なしなら NO_ACTIVE_SESSION を返す。</summary>
    [Fact]
    public async Task Screenshot_NoActiveSession_NoRef_ReturnsNoActiveSession()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.ScreenshotAsync();
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NoActiveSession, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>adact_screenshot: 未登録 sessionId は INVALID_ARGUMENT を返す。</summary>
    [Fact]
    public async Task Screenshot_UnknownSessionId_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.ScreenshotAsync(sessionId: "s99");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>adact_screenshot: 形式不正な ref は INVALID_ARGUMENT を返す。</summary>
    [Fact]
    public async Task Screenshot_MalformedRef_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.ScreenshotAsync(@ref: "bad");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>adact_screenshot: 未登録 session の ref は INVALID_ARGUMENT を返す。</summary>
    [Fact]
    public async Task Screenshot_UnknownRef_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.ScreenshotAsync(@ref: "s99e1");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>adact_screenshot: ref と sessionId の同時指定は INVALID_ARGUMENT を返す。</summary>
    [Fact]
    public async Task Screenshot_RefAndSessionIdTogether_ReturnsInvalidArgument()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.ScreenshotAsync(@ref: "s1e1", sessionId: "s1");
            var (code, message) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
            Assert.Contains("sessionId", message);
        }
        finally { store.Dispose(); }
    }
}
