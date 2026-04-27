using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Adact.Engine;
using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

/// <summary>
/// Phase 3 で公開する MCP ツール 5 種。
/// 詳細は 002_アーキテクチャ設計.md §4.1 / §6 / §8 参照。
/// </summary>
[McpServerToolType]
public sealed class WindowsTools
{
    private static readonly Regex WindowRefPattern = new("^w\\d+$", RegexOptions.Compiled);

    private readonly SessionStore _store;
    private readonly WindowRefStore _refStore;
    private readonly IDaemonControl _daemonControl;
    private readonly ILogger<WindowsTools> _logger;

    public WindowsTools(SessionStore store, WindowRefStore refStore, IDaemonControl daemonControl, ILogger<WindowsTools>? logger = null)
    {
        _store = store;
        _refStore = refStore;
        _daemonControl = daemonControl;
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
            var presentKeys = new List<WindowKey>(windows.Count);
            var arr = new JsonArray();
            foreach (var w in windows)
            {
                var key = WindowKey.From(w);
                presentKeys.Add(key);
                var entry = _refStore.SyncOrAssign(key, w);

                var o = new JsonObject
                {
                    ["windowRef"] = entry.WindowRef,
                };
                if (!string.IsNullOrEmpty(entry.SessionId)) o["sessionId"] = entry.SessionId;
                o["processName"] = w.ProcessName;
                o["processId"] = w.ProcessId;
                if (!string.IsNullOrEmpty(w.ClassName)) o["className"] = w.ClassName;
                o["windowTitle"] = w.Title;
                arr.Add(o);
            }
            _refStore.RetireMissing(presentKeys);

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
    [Description("Attach to a single top-level window. Specify windowRef (from windows_list_apps) OR any combination of processName / windowTitle / className / processId for strict-equal matching; multiple matches yield AMBIGUOUS_ATTACH. Returns sessionId (e.g. 's1'), windowRef and windowInfo.")]
    public async Task<CallToolResult> AttachAsync(
        [Description("Window Ref (e.g. 'w1') obtained from windows_list_apps. When specified, other matching parameters are ignored.")]
      string? windowRef = null,
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

        if (windowRef is null
            && processName is null && windowTitle is null && className is null && processId is null)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "windowRef must be specified, or at least one of processName / windowTitle / className / processId.");
        }

        try
        {
            WindowSession session;
            string assignedWindowRef;

            if (windowRef is not null)
            {
                if (!WindowRefPattern.IsMatch(windowRef))
                {
                    return ToolErrors.Error(ToolErrors.InvalidArgument,
                        $"Invalid windowRef format: '{windowRef}'. Expected pattern: w<n>.");
                }

                if (!_refStore.TryResolve(windowRef, out var entry))
                {
                    return ToolErrors.Error(ToolErrors.InvalidWindowRef,
                        $"Window Ref '{windowRef}' is unknown or has been retired. Re-run windows_list_apps.");
                }

                // idempotent: 既存 session が生きていればそれを返す
                if (entry.SessionId is not null
                    && _store.TryGet(entry.SessionId, out var existing))
                {
                    var existingInfo = new JsonObject
                    {
                        ["sessionId"] = entry.SessionId,
                        ["windowRef"] = entry.WindowRef,
                        ["windowInfo"] = new JsonObject
                        {
                            ["processName"] = existing.ProcessName,
                            ["windowTitle"] = existing.Title,
                            ["processId"] = existing.ProcessId,
                        },
                    };
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Text = existingInfo.ToJsonString() }],
                        StructuredContent = JsonSerializer.SerializeToElement(existingInfo),
                    };
                }

                session = await _store.Engine.AttachByHandleAsync(entry.Key.Hwnd, ct).ConfigureAwait(false);
                _store.Register(session);
                assignedWindowRef = entry.WindowRef;
                _refStore.AssociateSession(assignedWindowRef, $"s{session.SessionId}");
            }
            else
            {
                var query = new AttachQuery(processName, windowTitle, className, processId);

                // attach 前にマッチング対象を確定し、WindowKey で既存 entry を検索することで idempotent 化する。
                var matches = await _store.Engine.FindMatchesAsync(query, ct).ConfigureAwait(false);
                if (matches.Count == 0)
                    throw new WindowNotFoundException(query);
                if (matches.Count > 1)
                    throw new AmbiguousAttachException(query, matches);

                var target = matches[0];
                var key = WindowKey.From(target);

                // 既存 entry が生きている session を持っていれば idempotent: 既存 sessionId/windowRef を返す
                if (_refStore.TryFindByKey(key, out var found)
                    && !found.Retired
                    && found.SessionId is not null
                    && _store.TryGet(found.SessionId, out var existingSession))
                {
                    var existingInfo = new JsonObject
                    {
                        ["sessionId"] = found.SessionId,
                        ["windowRef"] = found.WindowRef,
                        ["windowInfo"] = new JsonObject
                        {
                            ["processName"] = existingSession.ProcessName,
                            ["windowTitle"] = existingSession.Title,
                            ["processId"] = existingSession.ProcessId,
                        },
                    };
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Text = existingInfo.ToJsonString() }],
                        StructuredContent = JsonSerializer.SerializeToElement(existingInfo),
                    };
                }

                session = await _store.Engine.AttachByHandleAsync(key.Hwnd, ct).ConfigureAwait(false);
                _store.Register(session);

                var entry = _refStore.SyncOrAssign(key, target);
                assignedWindowRef = entry.WindowRef;
                _refStore.AssociateSession(assignedWindowRef, $"s{session.SessionId}");
            }

            var info = new JsonObject
            {
                ["sessionId"] = $"s{session.SessionId}",
                ["windowRef"] = assignedWindowRef,
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

    [McpServerTool(Name = "windows_detach")]
    [Description("Release the session record without affecting the window or process. The session ID becomes invalid.")]
    public async Task<CallToolResult> DetachAsync(
        [Description("Session ID like 's1'. Omit to detach the active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (!TryResolveSessionId(sessionId, out var sid, out var error))
            return error!;

        if (!_store.TryRemove(sid, out var session))
            return ToolErrors.Error(ToolErrors.NotFound, $"Session '{sid}' not found.");

        DetachSession(sid, session);
        return SuccessJson(new JsonObject
        {
            ["sessionId"] = sid,
            ["detached"] = true,
        });
    }

    [McpServerTool(Name = "windows_close")]
    [Description("Close the attached window via UIA WindowPattern.Close() / WM_CLOSE. On success, the session is automatically detached.")]
    public async Task<CallToolResult> CloseAsync(
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (!TryResolveSessionId(sessionId, out var sid, out var error))
            return error!;
        if (!_store.TryGet(sid, out var session))
            return ToolErrors.Error(ToolErrors.NotFound, $"Session '{sid}' not found.");

        try
        {
            await session.CloseAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var mapped = ToolErrors.TryMap(ex);
            if (mapped is not null) return mapped;
            _logger.LogError(ex, "windows_close failed unexpectedly");
            throw;
        }

        if (_store.TryRemove(sid, out var removed))
        {
            DetachSession(sid, removed);
        }
        return SuccessJson(new JsonObject
        {
            ["sessionId"] = sid,
            ["closed"] = true,
            ["detached"] = true,
        });
    }

    [McpServerTool(Name = "windows_kill")]
    [Description("Forcefully terminate the process backing the attached window via Process.Kill. On success, the session is automatically detached.")]
    public async Task<CallToolResult> KillAsync(
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (!TryResolveSessionId(sessionId, out var sid, out var error))
            return error!;
        if (!_store.TryGet(sid, out var session))
            return ToolErrors.Error(ToolErrors.NotFound, $"Session '{sid}' not found.");

        try
        {
            await session.KillAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var mapped = ToolErrors.TryMap(ex);
            if (mapped is not null) return mapped;
            _logger.LogError(ex, "windows_kill failed unexpectedly");
            throw;
        }

        if (_store.TryRemove(sid, out var removed))
        {
            DetachSession(sid, removed);
        }
        return SuccessJson(new JsonObject
        {
            ["sessionId"] = sid,
            ["killed"] = true,
            ["detached"] = true,
        });
    }

    [McpServerTool(Name = "windows_close_all")]
    [Description("Close every attached window. Returns a per-session result array. Partial failure is reported, not thrown.")]
    public async Task<CallToolResult> CloseAllAsync(CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        var snapshot = _store.ListAll();
        var results = new JsonArray();

        foreach (var kv in snapshot)
        {
            var sid = kv.Key;
            var session = kv.Value;
            var entry = new JsonObject { ["sessionId"] = sid };

            try
            {
                await session.CloseAsync(ct).ConfigureAwait(false);
                if (_store.TryRemove(sid, out var removed))
                {
                    DetachSession(sid, removed);
                }
                entry["result"] = "ok";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (CloseFailedException ex)
            {
                entry["result"] = "fail";
                entry["error"] = ToolErrors.CloseFailed;
                entry["message"] = ex.Message;
                _logger.LogDebug(ex, "windows_close_all: closing session {Sid} failed", sid);
            }

            results.Add(entry);
        }

        return SuccessJson(new JsonObject { ["results"] = results });
    }

    [McpServerTool(Name = "daemon_stop")]
    [Description("Stop the daemon (HTTP listener). All sessions are detached first. Only available in HTTP mode.")]
    public async Task<CallToolResult> DaemonStopAsync(CancellationToken ct = default)
    {
        if (!_daemonControl.IsSupported)
        {
            return ToolErrors.Error(ToolErrors.LocalOnly,
                "daemon_stop is not supported in this mode.");
        }

        using (var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false))
        {
            // 全 session を detach (close ではない: 設計 §4.5)。
            foreach (var kv in _store.ListAll())
            {
                if (_store.TryRemove(kv.Key, out var removed))
                {
                    DetachSession(kv.Key, removed);
                }
            }
        }

        try
        {
            await _daemonControl.StopAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "daemon_stop: StopAsync failed");
            return ToolErrors.Error(ToolErrors.InternalError, ex.Message);
        }

        return SuccessJson(new JsonObject { ["stopped"] = true });
    }

    private bool TryResolveSessionId(string? sessionId, out string resolvedId, out CallToolResult? error)
    {
        if (sessionId is null)
        {
            var active = _store.ActiveSessionId;
            if (active is null)
            {
                error = ToolErrors.Error(ToolErrors.NoActiveSession,
                    "No active session. Call windows_attach first or specify sessionId explicitly.");
                resolvedId = string.Empty;
                return false;
            }
            resolvedId = active;
        }
        else
        {
            resolvedId = sessionId;
        }
        error = null;
        return true;
    }

    private void DetachSession(string sessionId, WindowSession session)
    {
        if (_refStore.TryFindBySessionId(sessionId, out var entry))
        {
            try { _refStore.ClearSession(entry.WindowRef); }
            catch (Exception ex) { _logger.LogDebug(ex, "ClearSession failed for {WindowRef}", entry.WindowRef); }
        }
        try { session.Dispose(); }
        catch (Exception ex) { _logger.LogDebug(ex, "Disposing session {Sid} failed", sessionId); }
    }

    private static CallToolResult SuccessJson(JsonObject obj)
    {
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = obj.ToJsonString() }],
            StructuredContent = JsonSerializer.SerializeToElement(obj),
        };
    }
}
