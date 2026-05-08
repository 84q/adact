using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine.Exceptions;

using ModelContextProtocol.Protocol;

namespace Adact.Mcp.Common;

/// <summary>
/// Creates MCP tool error responses.
/// </summary>
internal static class ToolErrors
{
    public const string InvalidArgument = "INVALID_ARGUMENT";
    public const string InvalidWindowRef = "INVALID_WINDOW_REF";
    public const string WindowNotFound = "WINDOW_NOT_FOUND";
    public const string RefNotFound = "REF_NOT_FOUND";
    public const string InvalidRefFormat = "INVALID_REF_FORMAT";
    public const string ElementInteractionFailed = "ELEMENT_INTERACTION_FAILED";
    public const string SnapshotFailed = "SNAPSHOT_FAILED";
    public const string NoActiveSession = "NO_ACTIVE_SESSION";
    public const string NotFound = "NOT_FOUND";
    public const string CloseFailed = "CLOSE_FAILED";
    public const string KillFailed = "KILL_FAILED";
    public const string LaunchFailed = "LAUNCH_FAILED";
    public const string LocalOnly = "LOCAL_ONLY";
    public const string InternalError = "INTERNAL_ERROR";
    public const string WaitTimeout = "WAIT_TIMEOUT";
    public const string OperationBlocked = "OPERATION_BLOCKED";

    public static CallToolResult? TryMap(Exception ex)
    {
        return ex switch
        {
            WindowNotFoundException w => Error(WindowNotFound, w.Message),
            RefNotFoundException r => Error(RefNotFound, r.Message,
                new JsonObject { ["refId"] = r.RefId }),
            ElementInteractionException e => Error(ElementInteractionFailed, e.Message),
            SnapshotException s => Error(SnapshotFailed, s.Message),
            CloseFailedException c => Error(CloseFailed, c.Message),
            KillFailedException k => Error(KillFailed, k.Message),
            LaunchFailedException l => Error(LaunchFailed, l.Message),
            WaitTimeoutException t => Error(WaitTimeout, t.Message),
            OperationBlockedException o => Error(OperationBlocked, o.Message),
            _ => null,
        };
    }

    /// <summary>
    /// Creates a structured MCP error response.
    /// </summary>
    public static CallToolResult Error(string code, string message, JsonObject? details = null)
    {
        var text = $"{code}: {message}";
        var structured = new JsonObject
        {
            ["code"] = code,
            ["message"] = message,
        };
        if (details is not null) structured["details"] = details;

        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = text }],
            StructuredContent = JsonSerializer.SerializeToElement(structured),
        };
    }
}
