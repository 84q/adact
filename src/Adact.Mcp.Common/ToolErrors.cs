using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine.Exceptions;

using ModelContextProtocol.Protocol;

namespace Adact.Mcp.Common;

/// <summary>
/// 業務例外 → MCP <see cref="CallToolResult"/> (isError:true) への変換を担うヘルパー。
/// systemic な例外は変換せず、呼び出し側で再 throw して SDK に JSON-RPC InternalError として処理させる。
/// 詳細は 002_アーキテクチャ設計.md §8 参照。
/// </summary>
internal static class ToolErrors
{
    public const string InvalidArgument = "INVALID_ARGUMENT";
    public const string InvalidWindowRef = "INVALID_WINDOW_REF";
    public const string WindowNotFound = "WINDOW_NOT_FOUND";
    public const string AmbiguousAttach = "AMBIGUOUS_ATTACH";
    public const string RefNotFound = "REF_NOT_FOUND";
    public const string ElementInteractionFailed = "ELEMENT_INTERACTION_FAILED";
    public const string SnapshotFailed = "SNAPSHOT_FAILED";
    public const string FilterStrategyNotFound = "FILTER_STRATEGY_NOT_FOUND";
    public const string NoActiveSession = "NO_ACTIVE_SESSION";
    public const string NotFound = "NOT_FOUND";
    public const string CloseFailed = "CLOSE_FAILED";
    public const string KillFailed = "KILL_FAILED";
    public const string LocalOnly = "LOCAL_ONLY";
    public const string InternalError = "INTERNAL_ERROR";

    /// <summary>業務例外なら <see cref="CallToolResult"/> を返し、それ以外は null。</summary>
    public static CallToolResult? TryMap(Exception ex)
    {
        return ex switch
        {
            WindowNotFoundException w => Error(WindowNotFound, w.Message),
            AmbiguousAttachException a => Error(AmbiguousAttach, a.Message,
                new JsonObject { ["candidateCount"] = a.Candidates.Count }),
            RefNotFoundException r => Error(RefNotFound, r.Message,
                new JsonObject { ["refId"] = r.RefId }),
            ElementInteractionException e => Error(ElementInteractionFailed, e.Message),
            SnapshotException s => Error(SnapshotFailed, s.Message),
            FilterStrategyNotFoundException f => Error(FilterStrategyNotFound, f.Message),
            CloseFailedException c => Error(CloseFailed, c.Message),
            KillFailedException k => Error(KillFailed, k.Message),
            _ => null,
        };
    }

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
