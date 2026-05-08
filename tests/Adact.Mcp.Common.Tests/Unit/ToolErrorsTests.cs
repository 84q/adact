using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine.Exceptions;

using ModelContextProtocol.Protocol;

using Xunit;

namespace Adact.Mcp.Common.Tests.Unit;

/// <summary>
/// Verifies MCP error result conversion for business exceptions.
/// </summary>
[Trait("Layer", "Unit")]
public class ToolErrorsTests
{
    /// <summary>
    /// Returns business exception samples and their expected MCP error codes.
    /// </summary>
    public static IEnumerable<object[]> BusinessExceptionCases()
    {
        yield return [new WindowNotFoundException(0x1234), ToolErrors.WindowNotFound];
        yield return [new RefNotFoundException("s1e2", "missing"), ToolErrors.RefNotFound];
        yield return [new ElementInteractionException("s1e2", "click", "not enabled"), ToolErrors.ElementInteractionFailed];
        yield return [new SnapshotException("snapshot failed"), ToolErrors.SnapshotFailed];
        yield return [new CloseFailedException("close failed"), ToolErrors.CloseFailed];
        yield return [new KillFailedException("kill failed"), ToolErrors.KillFailed];
        yield return [new LaunchFailedException("launch failed"), ToolErrors.LaunchFailed];
        yield return [new WaitTimeoutException("wait timed out"), ToolErrors.WaitTimeout];
        yield return [new OperationBlockedException("desktop locked", new InvalidOperationException("inner")), ToolErrors.OperationBlocked];
    }

    /// <summary>
    /// Business exceptions are mapped to isError results with code, message, and text content.
    /// </summary>
    [Theory]
    [MemberData(nameof(BusinessExceptionCases))]
    public void TryMap_BusinessExceptions_ReturnsStructuredError(Exception ex, string expectedCode)
    {
        var result = ToolErrors.TryMap(ex);

        Assert.NotNull(result);
        Assert.True(result.IsError == true);
        Assert.NotNull(result.StructuredContent);

        var structured = result.StructuredContent.Value;
        Assert.Equal(expectedCode, structured.GetProperty("code").GetString());
        Assert.Equal(ex.Message, structured.GetProperty("message").GetString());

        var text = Assert.Single(result.Content);
        var block = Assert.IsType<TextContentBlock>(text);
        Assert.Equal($"{expectedCode}: {ex.Message}", block.Text);
    }

    /// <summary>
    /// RefNotFoundException includes the unresolved ref id in details.
    /// </summary>
    [Fact]
    public void TryMap_RefNotFound_IncludesRefIdDetails()
    {
        var result = ToolErrors.TryMap(new RefNotFoundException("s9e8", "stale snapshot"));

        Assert.NotNull(result);
        Assert.NotNull(result.StructuredContent);
        var details = result.StructuredContent.Value.GetProperty("details");
        Assert.Equal("s9e8", details.GetProperty("refId").GetString());
    }

    /// <summary>
    /// Unknown exceptions are left unmapped for the host to treat as systemic errors.
    /// </summary>
    [Fact]
    public void TryMap_UnknownException_ReturnsNull()
    {
        var result = ToolErrors.TryMap(new InvalidOperationException("systemic failure"));

        Assert.Null(result);
    }

    /// <summary>
    /// Error omits details when no details object is supplied.
    /// </summary>
    [Fact]
    public void Error_WithoutDetails_OmitsDetailsProperty()
    {
        var result = ToolErrors.Error("CODE", "message");

        Assert.True(result.IsError == true);
        Assert.NotNull(result.StructuredContent);
        var structured = result.StructuredContent.Value;
        Assert.Equal("CODE", structured.GetProperty("code").GetString());
        Assert.Equal("message", structured.GetProperty("message").GetString());
        Assert.False(structured.TryGetProperty("details", out _));
    }

    /// <summary>
    /// Error preserves a supplied details object in structured content.
    /// </summary>
    [Fact]
    public void Error_WithDetails_EmbedsDetailsObject()
    {
        var result = ToolErrors.Error(
            "CODE",
            "message",
            new JsonObject
            {
                ["refId"] = "s1e2",
                ["candidateCount"] = 2,
            });

        Assert.True(result.IsError == true);
        Assert.NotNull(result.StructuredContent);
        var details = result.StructuredContent.Value.GetProperty("details");
        Assert.Equal(JsonValueKind.Object, details.ValueKind);
        Assert.Equal("s1e2", details.GetProperty("refId").GetString());
        Assert.Equal(2, details.GetProperty("candidateCount").GetInt32());
    }
}
