using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine;
using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Stdio;

/// <summary>
/// Phase 3 で公開する MCP ツール 5 種。
/// 詳細は 002_アーキテクチャ設計.md §4.1 / §6 / §8 参照。
/// </summary>
[McpServerToolType]
public sealed class WindowsTools
{
    private readonly SessionStore _store;
    private readonly ILogger<WindowsTools> _logger;

    public WindowsTools(SessionStore store, ILogger<WindowsTools>? logger = null)
    {
        _store = store;
        _logger = logger ?? NullLogger<WindowsTools>.Instance;
    }

    [McpServerTool(Name = "windows_list_apps")]
    [Description("List top-level windows currently running on this Windows desktop. Use this to discover candidates for windows_attach.")]
    public async Task<CallToolResult> ListAppsAsync(CancellationToken ct)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        try
        {
            var windows = await _store.Engine.ListWindowsAsync(ct).ConfigureAwait(false);
            var arr = new JsonArray();
            foreach (var w in windows)
            {
                var o = new JsonObject
                {
                    ["processName"] = w.ProcessName,
                    ["windowTitle"] = w.Title,
                    ["processId"] = w.ProcessId,
                };
                if (!string.IsNullOrEmpty(w.ClassName)) o["className"] = w.ClassName;
                arr.Add(o);
            }
            var json = arr.ToJsonString();
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = json }],
                StructuredContent = JsonSerializer.SerializeToElement(new JsonObject { ["windows"] = arr }),
            };
        }
        catch (Exception ex)
        {
            var mapped = ToolErrors.TryMap(ex);
            if (mapped is not null) return mapped;
            _logger.LogError(ex, "windows_list_apps failed unexpectedly");
            throw;
        }
    }

    [McpServerTool(Name = "windows_attach")]
    [Description("Attach to a single top-level window via strict-equal matching. Specify any combination of processName / windowTitle / className / processId; multiple matches yield AMBIGUOUS_ATTACH. Returns sessionId (e.g. 's1') and windowInfo. The attached session becomes the active session for subsequent windows_snapshot calls.")]
    public async Task<CallToolResult> AttachAsync(
        [Description("Process name (case-insensitive, exact match). Example: 'CalculatorApp', 'notepad++'.")]
      string? processName = null,
        [Description("Window title (case-insensitive, exact match). Example: '電卓'.")]
      string? windowTitle = null,
        [Description("Win32 ClassName (case-insensitive, exact match).")]
      string? className = null,
        [Description("Process ID.")]
      int? processId = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (processName is null && windowTitle is null && className is null && processId is null)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "At least one of processName / windowTitle / className / processId must be specified.");
        }

        try
        {
            var query = new AttachQuery(processName, windowTitle, className, processId);
            var session = await _store.Engine.AttachAsync(query, ct).ConfigureAwait(false);

            _store.Register(session);

            var info = new JsonObject
            {
                ["sessionId"] = $"s{session.SessionId}",
                ["windowInfo"] = new JsonObject
                {
                    ["processName"] = session.ProcessName,
                    ["windowTitle"] = session.Title,
                    ["processId"] = session.ProcessId,
                },
            };
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = info.ToJsonString() }],
                StructuredContent = JsonSerializer.SerializeToElement(info),
            };
        }
        catch (Exception ex)
        {
            var mapped = ToolErrors.TryMap(ex);
            if (mapped is not null) return mapped;
            _logger.LogError(ex, "windows_attach failed unexpectedly");
            throw;
        }
    }

    [McpServerTool(Name = "windows_snapshot")]
    [Description("Take a UIA snapshot of the attached window. When sessionId is omitted, the active session (last attached) is used. filter selects 'operable' (default, AI-friendly) or 'raw' (full UIA tree).")]
    public async Task<CallToolResult> SnapshotAsync(
        [Description("Session ID (e.g. 's1'). Omit to use the active session.")]
      string? sessionId = null,
        [Description("Filter strategy: 'operable' (default) or 'raw'.")]
      string? filter = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        WindowSession? session;
        if (sessionId is null)
        {
            session = _store.GetActiveOrNull();
            if (session is null)
            {
                return ToolErrors.Error(ToolErrors.NoActiveSession,
                    "No active session. Call windows_attach first or specify sessionId explicitly.");
            }
        }
        else if (!_store.TryGet(sessionId, out session))
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                $"Unknown sessionId '{sessionId}'.");
        }

        try
        {
            var options = new SnapshotOptions(FilterName: filter ?? "operable");
            var result = await session.SnapshotAsync(options, ct).ConfigureAwait(false);
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = result.Json }],
                StructuredContent = JsonSerializer.Deserialize<JsonElement>(result.Json),
            };
        }
        catch (Exception ex)
        {
            var mapped = ToolErrors.TryMap(ex);
            if (mapped is not null) return mapped;
            _logger.LogError(ex, "windows_snapshot failed unexpectedly");
            throw;
        }
    }

    [McpServerTool(Name = "windows_click")]
    [Description("Click an element identified by ref. The session is determined automatically from the ref id prefix.")]
    public async Task<CallToolResult> ClickAsync(
        [Description("Ref ID in the form 's<sid>g<gen>e<eid>' obtained from a recent windows_snapshot.")]
      string @ref,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrEmpty(@ref))
            return ToolErrors.Error(ToolErrors.InvalidArgument, "ref must be a non-empty string.");

        if (!RefId.TryParse(@ref, out _, out _, out _))
            return ToolErrors.Error(ToolErrors.RefNotFound, $"Ref ID '{@ref}' is malformed.");

        var session = _store.ResolveByRef(@ref);
        if (session is null)
            return ToolErrors.Error(ToolErrors.RefNotFound,
                $"Ref ID '{@ref}' does not match any known session.");

        try
        {
            await session.ClickAsync(@ref, options: null, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex)
        {
            var mapped = ToolErrors.TryMap(ex);
            if (mapped is not null) return mapped;
            _logger.LogError(ex, "windows_click failed unexpectedly");
            throw;
        }
    }

    [McpServerTool(Name = "windows_fill")]
    [Description("Fill (overwrite) an input element with the given value. The session is determined automatically from the ref id prefix.")]
    public async Task<CallToolResult> FillAsync(
        [Description("Ref ID in the form 's<sid>g<gen>e<eid>' obtained from a recent windows_snapshot.")]
      string @ref,
        [Description("Text value to set.")]
      string value,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrEmpty(@ref))
            return ToolErrors.Error(ToolErrors.InvalidArgument, "ref must be a non-empty string.");
        if (value is null)
            return ToolErrors.Error(ToolErrors.InvalidArgument, "value must not be null.");

        if (!RefId.TryParse(@ref, out _, out _, out _))
            return ToolErrors.Error(ToolErrors.RefNotFound, $"Ref ID '{@ref}' is malformed.");

        var session = _store.ResolveByRef(@ref);
        if (session is null)
            return ToolErrors.Error(ToolErrors.RefNotFound,
                $"Ref ID '{@ref}' does not match any known session.");

        try
        {
            await session.FillAsync(@ref, value, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex)
        {
            var mapped = ToolErrors.TryMap(ex);
            if (mapped is not null) return mapped;
            _logger.LogError(ex, "windows_fill failed unexpectedly");
            throw;
        }
    }
}
