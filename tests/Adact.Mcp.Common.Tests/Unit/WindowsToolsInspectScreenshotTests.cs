using Adact.Engine;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>Contains tests for the Windows Tools Inspect Screenshot behavior.</summary>
[Trait("Layer", "Unit")]
public class WindowsToolsInspectScreenshotTests
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

    /// <summary>Performs the Inspect Empty Ref Returns Invalid Argument operation.</summary>
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

    /// <summary>Performs the Inspect Malformed Ref Returns Ref Not Found operation.</summary>
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

    /// <summary>Performs the Inspect Unknown Session Returns Ref Not Found operation.</summary>
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

    /// <summary>Performs the Screenshot No Active Session No Ref Returns No Active Session operation.</summary>
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

    /// <summary>Performs the Screenshot Unknown Session Id Returns Invalid Argument operation.</summary>
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

    /// <summary>Performs the Screenshot Malformed Ref Returns Invalid Argument operation.</summary>
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

    /// <summary>Performs the Screenshot Unknown Ref Returns Invalid Argument operation.</summary>
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

    /// <summary>Performs the Screenshot Ref And Session Id Together Returns Invalid Argument operation.</summary>
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
