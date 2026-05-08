using Adact.Engine;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>Contains tests for the Windows Tools Window behavior.</summary>
[Trait("Layer", "Unit")]
public class WindowsToolsWindowTests
{
    private sealed class FakeDaemonControl : IDaemonControl
    {
        /// <summary>Gets a value indicating whether Is Supported.</summary>
        public bool IsSupported { get; init; } = true;
        /// <summary>Performs the Stop Async operation.</summary>
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

    /// <summary>Performs the Resize Non Positive Width Returns Invalid Argument operation.</summary>
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

    /// <summary>Performs the Resize Non Positive Height Returns Invalid Argument operation.</summary>
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

    /// <summary>Performs the Resize Both Zero Returns Invalid Argument For Width operation.</summary>
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

    /// <summary>Performs the Resize Valid Args No Active Session Returns No Active Session operation.</summary>
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

    /// <summary>Performs the Minimize No Active Session Returns No Active Session operation.</summary>
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

    /// <summary>Performs the Minimize Unknown Session Id Returns Not Found operation.</summary>
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

    /// <summary>Performs the Maximize No Active Session Returns No Active Session operation.</summary>
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

    /// <summary>Performs the Maximize Unknown Session Id Returns Not Found operation.</summary>
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

    /// <summary>Performs the Restore No Active Session Returns No Active Session operation.</summary>
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

    /// <summary>Performs the Restore Unknown Session Id Returns Not Found operation.</summary>
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
