using Adact.Engine;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>
/// WindowsTools の Window 系メソッド (windows_resize / windows_minimize / windows_maximize / windows_restore)
/// の引数検証および session 解決エラー (Phase 8 Step 5) を検証する Unit テスト。
/// 実 UIA を呼ばない範囲 (引数検証 / セッション未解決) のみを対象とし、成功パスは L3 IntegrationUia /
/// L4 Smoke で別途検証する。
/// </summary>
[Trait("Layer", "Unit")]
public class WindowsToolsWindowTests
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

    /// <summary>
    /// width が 0 以下の場合、windows_resize は INVALID_ARGUMENT を返すことを確認する。
    /// session 解決前に引数検証が走る契約の回帰防止。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Resize_NonPositiveWidth_ReturnsInvalidArgument(int width)
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.ResizeAsync(width: width, height: 100);
            var (code, msg) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
            Assert.Contains("width", msg, StringComparison.Ordinal);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// height が 0 以下の場合、windows_resize は INVALID_ARGUMENT を返すことを確認する。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Resize_NonPositiveHeight_ReturnsInvalidArgument(int height)
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.ResizeAsync(width: 100, height: height);
            var (code, msg) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
            Assert.Contains("height", msg, StringComparison.Ordinal);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// width=0, height=0 の場合、最初に評価される width 検証で INVALID_ARGUMENT を返すことを確認する。
    /// </summary>
    [Fact]
    public async Task Resize_BothZero_ReturnsInvalidArgumentForWidth()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.ResizeAsync(width: 0, height: 0);
            var (code, msg) = ReadError(result);
            Assert.Equal(ToolErrors.InvalidArgument, code);
            Assert.Contains("width", msg, StringComparison.Ordinal);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// 有効な width/height だが active session が無く sessionId も未指定の場合、
    /// windows_resize は NO_ACTIVE_SESSION を返すことを確認する。
    /// </summary>
    [Fact]
    public async Task Resize_ValidArgsNoActiveSession_ReturnsNoActiveSession()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.ResizeAsync(width: 100, height: 100, sessionId: null);
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NoActiveSession, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// active session が無く sessionId 未指定の場合、windows_minimize は NO_ACTIVE_SESSION を返すことを確認する。
    /// </summary>
    [Fact]
    public async Task Minimize_NoActiveSession_ReturnsNoActiveSession()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.MinimizeAsync(sessionId: null);
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NoActiveSession, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// 未登録 sessionId で windows_minimize を呼ぶと NOT_FOUND を返すことを確認する。
    /// </summary>
    [Fact]
    public async Task Minimize_UnknownSessionId_ReturnsNotFound()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.MinimizeAsync(sessionId: "s99");
            var (code, msg) = ReadError(result);
            Assert.Equal(ToolErrors.NotFound, code);
            Assert.Contains("s99", msg, StringComparison.Ordinal);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// active session が無く sessionId 未指定の場合、windows_maximize は NO_ACTIVE_SESSION を返すことを確認する。
    /// </summary>
    [Fact]
    public async Task Maximize_NoActiveSession_ReturnsNoActiveSession()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.MaximizeAsync(sessionId: null);
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NoActiveSession, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// 未登録 sessionId で windows_maximize を呼ぶと NOT_FOUND を返すことを確認する。
    /// </summary>
    [Fact]
    public async Task Maximize_UnknownSessionId_ReturnsNotFound()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.MaximizeAsync(sessionId: "s42");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NotFound, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// active session が無く sessionId 未指定の場合、windows_restore は NO_ACTIVE_SESSION を返すことを確認する。
    /// </summary>
    [Fact]
    public async Task Restore_NoActiveSession_ReturnsNoActiveSession()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.RestoreAsync(sessionId: null);
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NoActiveSession, code);
        }
        finally { store.Dispose(); }
    }

    /// <summary>
    /// 未登録 sessionId で windows_restore を呼ぶと NOT_FOUND を返すことを確認する。
    /// </summary>
    [Fact]
    public async Task Restore_UnknownSessionId_ReturnsNotFound()
    {
        var (tools, store) = CreateTools();
        try
        {
            var result = await tools.RestoreAsync(sessionId: "s7");
            var (code, _) = ReadError(result);
            Assert.Equal(ToolErrors.NotFound, code);
        }
        finally { store.Dispose(); }
    }
}
