using Adact.Engine;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>Contains tests for the Windows Tools Wait For behavior.</summary>
[Trait("Layer", "Unit")]
public class WindowsToolsWaitForTests
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

    /// <summary>Waits for the Wait For Ref And Query Both Returns Invalid Argument condition.</summary>
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

    /// <summary>Waits for the Wait For No Conditions Returns Invalid Argument condition.</summary>
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

    /// <summary>Waits for the Wait For Unknown State Returns Invalid Argument condition.</summary>
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

    /// <summary>Waits for the Wait For Non Positive Timeout Returns Invalid Argument condition.</summary>
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

    /// <summary>Waits for the Wait For Negative Timeout Returns Invalid Argument condition.</summary>
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

    /// <summary>Waits for the Wait For Ref With Session Id Returns Invalid Argument condition.</summary>
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

    /// <summary>Waits for the Wait For Malformed Ref Returns Ref Not Found condition.</summary>
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

    /// <summary>Waits for the Wait For Unknown Session Ref Returns Ref Not Found condition.</summary>
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

    /// <summary>Waits for the Wait For Query Mode No Active Session Returns No Active Session condition.</summary>
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

    /// <summary>Waits for the Wait For Query Mode Unknown Session Id Returns Not Found condition.</summary>
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

    /// <summary>Waits for the Wait For Window No Conditions Returns Invalid Argument condition.</summary>
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

    /// <summary>Waits for the Wait For Window Non Positive Timeout Returns Invalid Argument condition.</summary>
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

    /// <summary>Waits for the Wait For Window Negative Timeout Returns Invalid Argument condition.</summary>
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

    /// <summary>Performs the Tool Errors Wait Timeout Has Expected Wire Value operation.</summary>
    [Fact]
    public void ToolErrors_WaitTimeout_HasExpectedWireValue()
    {
        Assert.Equal("WAIT_TIMEOUT", ToolErrors.WaitTimeout);
    }

    /// <summary>Performs the Tool Errors Try Map Maps Wait Timeout Exception operation.</summary>
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
