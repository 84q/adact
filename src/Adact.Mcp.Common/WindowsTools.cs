using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Adact.Engine;
using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;
using Adact.Mcp.Common.InputDrivers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

/// <summary>
/// MCP tools for listing, attaching to, and controlling windows.
/// </summary>
[McpServerToolType]
public sealed partial class WindowsTools
{
    private static readonly Regex WindowRefPattern = new("^w\\d+$", RegexOptions.Compiled);

    private const int DefaultKillTimeoutMs = 5000;

    private readonly SessionStore _store;
    private readonly WindowRefStore _refStore;
    private readonly IDaemonControl _daemonControl;
    private readonly ILogger<WindowsTools> _logger;
    private readonly IMouseDriver _mouseDriver;
    private readonly IKeyboardDriver _keyboardDriver;


    /// <summary>
    /// Creates a new windows tool set.
    /// </summary>
    public WindowsTools(
        SessionStore store,
        WindowRefStore refStore,
        IDaemonControl daemonControl,
        ILogger<WindowsTools>? logger = null,
        IMouseDriver? mouseDriver = null,
        IKeyboardDriver? keyboardDriver = null)
    {
        _store = store;
        _refStore = refStore;
        _daemonControl = daemonControl;
        _logger = logger ?? NullLogger<WindowsTools>.Instance;
        _mouseDriver = mouseDriver ?? new FlaUiMouseDriver();
        _keyboardDriver = keyboardDriver ?? new FlaUiKeyboardDriver();
    }

    /// <summary>
    /// Lists top-level windows on the current desktop.
    /// </summary>
    [McpServerTool(Name = "adact_list_windows")]
    [Description("List top-level windows currently running on this Windows desktop. Use this to discover candidates for adact_attach.")]
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
            _logger.LogError(ex, "adact_list_windows failed unexpectedly");
            throw;
        }
    }

    /// <summary>
    /// Attaches to a top-level window by window reference.
    /// </summary>
    [McpServerTool(Name = "adact_attach")]
    [Description("Attach to a single top-level window identified by a windowRef obtained from adact_list_windows. Returns sessionId (e.g. 's1'), windowRef and windowInfo.")]
    public async Task<CallToolResult> AttachAsync(
        [Description("Window Ref (e.g. 'w1') obtained from adact_list_windows.")]
      string windowRef,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrEmpty(windowRef))
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "windowRef must be a non-empty string in the form 'w<n>'.");
        }
        if (!WindowRefPattern.IsMatch(windowRef))
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                $"Invalid windowRef format: '{windowRef}'. Expected pattern: w<n>.");
        }
        if (!_refStore.TryResolve(windowRef, out var entry))
        {
            return ToolErrors.Error(ToolErrors.InvalidWindowRef,
                $"Window Ref '{windowRef}' is unknown or has been retired. Re-run adact_list_windows.");
        }

        try
        {
            IWindowSession session;
            if (entry.SessionId is { } sid && _store.TryGet(sid, out var live))
            {
                session = live;
            }
            else
            {
                session = await _store.Engine.AttachByHandleAsync(entry.Key.Hwnd, ct).ConfigureAwait(false);
                _store.Register(session);
                _refStore.AssociateSession(entry.WindowRef, $"s{session.SessionId}");
            }

            var result = new JsonObject
            {
                ["sessionId"] = $"s{session.SessionId}",
                ["windowRef"] = entry.WindowRef,
                ["windowInfo"] = new JsonObject
                {
                    ["processName"] = session.ProcessName,
                    ["windowTitle"] = session.Title,
                    ["processId"] = session.ProcessId,
                },
            };
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = result.ToJsonString() }],
                StructuredContent = JsonSerializer.SerializeToElement(result),
            };
        }
        catch (Exception ex)
        {
            var mapped = ToolErrors.TryMap(ex);
            if (mapped is not null) return mapped;
            _logger.LogError(ex, "adact_attach failed unexpectedly");
            throw;
        }
    }

    /// <summary>
    /// Captures a snapshot of the active session.
    /// </summary>
    [McpServerTool(Name = "adact_snapshot")]
    [Description("Take a UIA snapshot of the attached window. Returns the raw UIA tree as JSON with all elements and properties; filtering and field selection are performed client-side. When sessionId is omitted, the active session (last attached) is used.")]
    public async Task<CallToolResult> SnapshotAsync(
        [Description("Session ID (e.g. 's1'). Omit to use the active session.")]
      string? sessionId = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        IWindowSession? session;
        if (sessionId is null)
        {
            session = _store.GetActiveOrNull();
            if (session is null)
            {
                return ToolErrors.Error(ToolErrors.NoActiveSession,
                    "No active session. Call adact_attach first or specify sessionId explicitly.");
            }
        }
        else if (!_store.TryGet(sessionId, out session))
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                $"Unknown sessionId '{sessionId}'.");
        }

        try
        {
            var result = await session.SnapshotAsync(options: null, ct).ConfigureAwait(false);
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
            _logger.LogError(ex, "adact_snapshot failed unexpectedly");
            throw;
        }
    }

    /// <summary>
    /// Clicks an element identified by ref.
    /// </summary>
    [McpServerTool(Name = "adact_click")]
    [Description("Click an element identified by ref. The session is determined automatically from the ref id prefix.")]
    public async Task<CallToolResult> ClickAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent adact_snapshot.")]
      string @ref,
        [Description("Mouse button: 'left' (default), 'right', or 'middle'.")]
        string? button = null,
        [Description("Number of consecutive clicks (>= 1). Defaults to 1.")]
        int? count = null,
        [Description("Modifier keys held during the click. Allowed: 'Shift', 'Control', 'Ctrl', 'Alt', 'Meta', 'Win', 'Windows'.")]
        IReadOnlyList<string>? modifiers = null,
        [Description("X offset (px) from the element's bounding-rectangle top-left. Omit to click center.")]
        int? positionX = null,
        [Description("Y offset (px) from the element's bounding-rectangle top-left. Omit to click center.")]
        int? positionY = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrEmpty(@ref))
            return ToolErrors.Error(ToolErrors.InvalidArgument, "ref must be a non-empty string.");

        if (!RefId.TryParse(@ref, out _, out _))
            return ToolErrors.Error(ToolErrors.RefNotFound, $"Ref ID '{@ref}' is malformed.");

        var session = _store.ResolveByRef(@ref);
        if (session is null)
            return ToolErrors.Error(ToolErrors.RefNotFound,
                $"Ref ID '{@ref}' does not match any known session.");

        if (count is { } cnt && cnt < 1)
            return ToolErrors.Error(ToolErrors.InvalidArgument, "count must be >= 1.");

        if (!TryParseMouseButton(button, out var btn, out var btnError))
            return ToolErrors.Error(ToolErrors.InvalidArgument, btnError);

        try
        {
            bool hasExtensions = button is not null || count is not null
                || (modifiers is { Count: > 0 })
                || positionX is not null || positionY is not null;
            if (!hasExtensions)
            {
                await session.ClickAsync(@ref, options: null, ct).ConfigureAwait(false);
            }
            else
            {
                var opts = new ClickOptions(
                    Double: false,
                    Button: btn,
                    Count: count ?? 1,
                    Modifiers: modifiers,
                    PositionX: positionX,
                    PositionY: positionY);
                await session.ClickWithOptionsAsync(@ref, opts, ct).ConfigureAwait(false);
            }
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex)
        {
            var mapped = ToolErrors.TryMap(ex);
            if (mapped is not null) return mapped;
            _logger.LogError(ex, "adact_click failed unexpectedly");
            throw;
        }
    }

    internal static bool TryParseMouseButton(string? button, out MouseButton result, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrEmpty(button)) { result = MouseButton.Left; return true; }
        switch (button.Trim().ToLowerInvariant())
        {
            case "left": result = MouseButton.Left; return true;
            case "right": result = MouseButton.Right; return true;
            case "middle": result = MouseButton.Middle; return true;
            default:
                result = MouseButton.Left;
                error = $"button '{button}' is not one of 'left', 'right', 'middle'.";
                return false;
        }
    }

    /// <summary>
    /// Types text into an element or the active window.
    /// </summary>
    [McpServerTool(Name = "adact_fill")]
    [Description("Fill (overwrite) an input element with the given value. The session is determined automatically from the ref id prefix.")]
    public async Task<CallToolResult> FillAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent adact_snapshot.")]
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

        if (!RefId.TryParse(@ref, out _, out _))
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
            _logger.LogError(ex, "adact_fill failed unexpectedly");
            throw;
        }
    }

    /// <summary>
    /// Detaches a session from the active window reference.
    /// </summary>
    [McpServerTool(Name = "adact_detach")]
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

    /// <summary>
    /// Closes the attached window.
    /// </summary>
    [McpServerTool(Name = "adact_close_window")]
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
            _logger.LogError(ex, "adact_close_window failed unexpectedly");
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

    /// <summary>
    /// Kills the attached process.
    /// </summary>
    [McpServerTool(Name = "adact_kill")]
    [Description("Terminate the process backing the attached window. By default sends WM_CLOSE and waits for graceful exit; falls back to Process.Kill on timeout. Use force=true to skip WM_CLOSE and kill immediately.")]
    public async Task<CallToolResult> KillAsync(
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        [Description("Skip WM_CLOSE and immediately kill the process (like the old behavior).")]
        bool force = false,
        [Description("Graceful shutdown timeout in milliseconds. Defaults to 5000.")]
        int? timeoutMs = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (!TryResolveSessionId(sessionId, out var sid, out var error))
            return error!;
        if (!_store.TryGet(sid, out var session))
            return ToolErrors.Error(ToolErrors.NotFound, $"Session '{sid}' not found.");

        KillMethod method;
        try
        {
            method = await session.KillAsync(force, timeoutMs ?? DefaultKillTimeoutMs, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var mapped = ToolErrors.TryMap(ex);
            if (mapped is not null) return mapped;
            _logger.LogError(ex, "adact_kill failed unexpectedly");
            throw;
        }

        if (_store.TryRemove(sid, out var removed))
        {
            DetachSession(sid, removed);
        }

        var methodStr = method switch
        {
            KillMethod.Graceful => "graceful",
            KillMethod.ForcedAfterTimeout => "forced_after_timeout",
            _ => "forced",
        };

        return SuccessJson(new JsonObject
        {
            ["sessionId"] = sid,
            ["killed"] = true,
            ["detached"] = true,
            ["method"] = methodStr,
        });
    }

    /// <summary>
    /// Stops the daemon.
    /// </summary>
    [McpServerTool(Name = "adact_daemon_stop")]
    [Description("Stop the daemon (HTTP listener). All sessions are detached first. Only available in HTTP mode.")]
    public async Task<CallToolResult> DaemonStopAsync(CancellationToken ct = default)
    {
        if (!_daemonControl.IsSupported)
        {
            return ToolErrors.Error(ToolErrors.LocalOnly,
                "adact_daemon_stop is not supported in this mode.");
        }

        using (var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false))
        {
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
            _logger.LogError(ex, "adact_daemon_stop: StopAsync failed");
            return ToolErrors.Error(ToolErrors.InternalError, ex.Message);
        }

        return SuccessJson(new JsonObject { ["stopped"] = true });
    }

    /// <summary>
    /// </summary>
    private bool TryResolveSessionId(string? sessionId, out string resolvedId, out CallToolResult? error)
    {
        if (sessionId is null)
        {
            var active = _store.ActiveSessionId;
            if (active is null)
            {
                error = ToolErrors.Error(ToolErrors.NoActiveSession,
                    "No active session. Call adact_attach first or specify sessionId explicitly.");
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

    private static string? GetToolErrorCode(Exception ex)
    {
        var mapped = ToolErrors.TryMap(ex);
        if (mapped?.StructuredContent is not JsonElement structured)
        {
            return null;
        }

        return structured.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String
            ? code.GetString()
            : null;
    }

    /// <summary>
    /// </summary>
    private void DetachSession(string sessionId, IWindowSession session)
    {
        try { _refStore.RemoveBySessionId(sessionId); }
        catch (Exception ex) { _logger.LogDebug(ex, "RemoveBySessionId failed for {SessionId}", sessionId); }
        try { session.Dispose(); }
        catch (Exception ex) { _logger.LogDebug(ex, "Disposing session {Sid} failed", sessionId); }
    }

    /// <summary>
    /// </summary>
    private static CallToolResult SuccessJson(JsonObject obj)
    {
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = obj.ToJsonString() }],
            StructuredContent = JsonSerializer.SerializeToElement(obj),
        };
    }
}
