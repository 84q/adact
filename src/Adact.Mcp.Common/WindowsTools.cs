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
/// Phase 3 以降で公開する MCP ツール群 (windows_* と daemon_stop)。
/// 詳細は docs/spec/mcp-tools.md および discussion/002_アーキテクチャ設計.md §4.1 / §6 / §8 を参照。
/// </summary>
[McpServerToolType]
public sealed partial class WindowsTools
{
    /// <summary><see cref="AttachAsync"/> で受け取る <c>windowRef</c> の文法 (<c>w</c> + 1 桁以上の数字) を検証する正規表現。</summary>
    private static readonly Regex WindowRefPattern = new("^w\\d+$", RegexOptions.Compiled);

    /// <summary>window session を sessionId で管理し、ツール呼び出しを直列化するストア。</summary>
    private readonly SessionStore _store;
    /// <summary>top-level window に対する <c>w&lt;n&gt;</c> ref の発行・同期を担うストア。</summary>
    private readonly WindowRefStore _refStore;
    /// <summary>daemon プロセス停止 (<c>daemon_stop</c>) を抽象化したアダプタ。stdio モードでは <see cref="IDaemonControl.IsSupported"/> が <c>false</c>。</summary>
    private readonly IDaemonControl _daemonControl;
    /// <summary>業務例外以外の予期せぬ失敗を記録するロガー。</summary>
    private readonly ILogger<WindowsTools> _logger;
    /// <summary>低レベルマウス操作の抽象化 (テスト時に差し替え可能)。</summary>
    private readonly IMouseDriver _mouseDriver;
    /// <summary>低レベルキーボード操作の抽象化 (テスト時に差し替え可能)。</summary>
    private readonly IKeyboardDriver _keyboardDriver;


    /// <summary>
    /// MCP ツール群を構築する。stdio / HTTP どちらのホストからも同じ実装が共有される。
    /// </summary>
    /// <param name="store">UIA 直列化 lock と sessionId 辞書を保持する <see cref="SessionStore"/>。</param>
    /// <param name="refStore"><c>w&lt;n&gt;</c> ref を発行・同期する <see cref="WindowRefStore"/>。</param>
    /// <param name="daemonControl"><c>daemon_stop</c> を実行するためのモード固有の実装。</param>
    /// <param name="logger">未マップ例外用ロガー。<c>null</c> の場合は <see cref="NullLogger{T}"/> を使用する。</param>
    /// <param name="mouseDriver">低レベルマウス操作の実装。省略時は FlaUI 本番実装。</param>
    /// <param name="keyboardDriver">低レベルキーボード操作の実装。省略時は FlaUI 本番実装。</param>
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
    /// 現在のデスクトップに存在する top-level window を列挙し、各 window に <c>w&lt;n&gt;</c> ref を割り当てて返す。
    /// </summary>
    /// <param name="ct">キャンセル トークン。</param>
    /// <returns>
    /// <c>windows</c> 配列を含む <see cref="CallToolResult"/>。各要素は <c>windowRef</c> / <c>sessionId</c> (attach 済みのみ) /
    /// <c>processName</c> / <c>processId</c> / <c>className</c> / <c>windowTitle</c> を持つ。
    /// </returns>
    /// <remarks>
    /// 同一 <see cref="WindowKey"/> には常に同じ <c>w&lt;n&gt;</c> を再利用する。前回 list に含まれていたが今回消えた window は
    /// <see cref="WindowRefStore.RetireMissing"/> により retired となり、以降 <see cref="AttachAsync"/> で解決できなくなる。
    /// すべてのツール呼び出しは <see cref="SessionStore"/> の semaphore で直列化される。
    /// </remarks>
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

    /// <summary>
    /// 指定された <c>windowRef</c> に対応する単一 window へ attach し、sessionId / windowRef / windowInfo を返す。
    /// </summary>
    /// <param name="windowRef"><see cref="ListAppsAsync"/> で得た <c>w&lt;n&gt;</c>。必須。</param>
    /// <param name="ct">キャンセル トークン。</param>
    /// <returns>
    /// <c>sessionId</c> / <c>windowRef</c> / <c>windowInfo</c> を含む <see cref="CallToolResult"/>。
    /// 形式不正は <c>INVALID_ARGUMENT</c>、未知 / 引退済みの <c>w&lt;n&gt;</c> は <c>INVALID_WINDOW_REF</c>、
    /// HWND からの attach に失敗した場合は <c>WINDOW_NOT_FOUND</c> を返す。
    /// </returns>
    /// <remarks>
    /// 同じ window に対する再 attach は既存 session を返し、二重に sessionId を発行しない。
    /// </remarks>
    [McpServerTool(Name = "windows_attach")]
    [Description("Attach to a single top-level window identified by a windowRef obtained from windows_list_apps. Returns sessionId (e.g. 's1'), windowRef and windowInfo.")]
    public async Task<CallToolResult> AttachAsync(
        [Description("Window Ref (e.g. 'w1') obtained from windows_list_apps.")]
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
                $"Window Ref '{windowRef}' is unknown or has been retired. Re-run windows_list_apps.");
        }

        try
        {
            // session 確保: 既存があれば再利用、なければ新規 attach
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

            // 結果構築
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
            _logger.LogError(ex, "windows_attach failed unexpectedly");
            throw;
        }
    }

    /// <summary>
    /// 指定 session (省略時はアクティブ session) で UIA tree を走査し、各要素に <c>s&lt;sid&gt;e&lt;eid&gt;</c> ref を付与した
    /// snapshot JSON を返す。
    /// </summary>
    /// <param name="sessionId">対象 session。省略するとアクティブ session を使う。アクティブが無ければ <c>NO_ACTIVE_SESSION</c>。</param>
    /// <param name="ct">キャンセル トークン。</param>
    /// <returns>
    /// snapshot JSON を text content と structured content の両方で含む <see cref="CallToolResult"/>。
    /// 不明な <paramref name="sessionId"/> は <c>INVALID_ARGUMENT</c> を返す。
    /// </returns>
    [McpServerTool(Name = "windows_snapshot")]
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
            _logger.LogError(ex, "windows_snapshot failed unexpectedly");
            throw;
        }
    }

    /// <summary>
    /// 直近 snapshot で得た element ref が指す UIA 要素を click する。session は ref の prefix (<c>s&lt;sid&gt;</c>) から自動解決する。
    /// </summary>
    /// <param name="ref">snapshot 由来の element ref (例: <c>s1e7</c>)。</param>
    /// <param name="button">"left"/"right"/"middle"。null は "left"。</param>
    /// <param name="count">連打回数 (>=1)。null は 1。</param>
    /// <param name="modifiers">押下する修飾キー名 (Shift/Control/Ctrl/Alt/Meta/Win/Windows)。</param>
    /// <param name="positionX">要素左上基準 X オフセット (px)。null で中央。</param>
    /// <param name="positionY">要素左上基準 Y オフセット (px)。null で中央。</param>
    /// <param name="ct">キャンセル トークン。</param>
    /// <returns>
    /// 成功時は空の content を持つ <see cref="CallToolResult"/>。ref が空・不正・未知 session prefix の場合は
    /// <c>INVALID_ARGUMENT</c> または <c>REF_NOT_FOUND</c>、要素操作失敗時は <c>ELEMENT_INTERACTION_FAILED</c>。
    /// </returns>
    [McpServerTool(Name = "windows_click")]
    [Description("Click an element identified by ref. The session is determined automatically from the ref id prefix.")]
    public async Task<CallToolResult> ClickAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent windows_snapshot.")]
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
            // 既存挙動を維持: 拡張パラメータが一切指定されていなければ ClickAsync(options:null) を呼ぶ。
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
            _logger.LogError(ex, "windows_click failed unexpectedly");
            throw;
        }
    }

    /// <summary>"left"/"right"/"middle" 文字列を <see cref="MouseButton"/> に解釈する。null は Left 扱い。</summary>
    /// <param name="button">"left"/"right"/"middle"/null。大文字小文字は無視。</param>
    /// <param name="result">解釈結果 (デフォルトは <see cref="MouseButton.Left"/>)。</param>
    /// <param name="error">エラーメッセージ (失敗時のみ)。</param>
    /// <returns>成功時 true。</returns>
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
    /// element ref が指す入力要素のテキストを <paramref name="value"/> で完全に上書きする。
    /// session は ref の prefix から自動解決する。
    /// </summary>
    /// <param name="ref">snapshot 由来の element ref。</param>
    /// <param name="value">設定するテキスト (空文字列は許可、<c>null</c> は <c>INVALID_ARGUMENT</c>)。</param>
    /// <param name="ct">キャンセル トークン。</param>
    /// <returns>
    /// 成功時は空の content を持つ <see cref="CallToolResult"/>。
    /// ref が空・不正なら <c>INVALID_ARGUMENT</c> / <c>REF_NOT_FOUND</c>、操作失敗時は <c>ELEMENT_INTERACTION_FAILED</c>。
    /// </returns>
    [McpServerTool(Name = "windows_fill")]
    [Description("Fill (overwrite) an input element with the given value. The session is determined automatically from the ref id prefix.")]
    public async Task<CallToolResult> FillAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent windows_snapshot.")]
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
            _logger.LogError(ex, "windows_fill failed unexpectedly");
            throw;
        }
    }

    /// <summary>
    /// 指定 session を <see cref="SessionStore"/> から取り外す。window やプロセスには影響を与えず、sessionId のみ無効化する。
    /// </summary>
    /// <param name="sessionId">対象 session。省略するとアクティブ session を detach する。</param>
    /// <param name="ct">キャンセル トークン。</param>
    /// <returns>
    /// <c>{ sessionId, detached: true }</c> を含む <see cref="CallToolResult"/>。アクティブが無い場合は <c>NO_ACTIVE_SESSION</c>、
    /// session が見つからない場合は <c>NOT_FOUND</c>。
    /// </returns>
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

    /// <summary>
    /// UIA <c>WindowPattern.Close()</c> もしくは <c>WM_CLOSE</c> 経由で window を閉じる。成功時は同 session を自動的に detach する。
    /// </summary>
    /// <param name="sessionId">対象 session。省略するとアクティブ session を使う。</param>
    /// <param name="ct">キャンセル トークン。</param>
    /// <returns>
    /// <c>{ sessionId, closed: true, detached: true }</c> を含む <see cref="CallToolResult"/>。
    /// アクティブが無い場合は <c>NO_ACTIVE_SESSION</c>、session 不明は <c>NOT_FOUND</c>、close 失敗は <c>CLOSE_FAILED</c>。
    /// </returns>
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

    /// <summary>
    /// <c>Process.Kill</c> によって window の背後にあるプロセスを強制終了する。成功時は session を自動的に detach する。
    /// </summary>
    /// <param name="sessionId">対象 session。省略するとアクティブ session を使う。</param>
    /// <param name="ct">キャンセル トークン。</param>
    /// <returns>
    /// <c>{ sessionId, killed: true, detached: true }</c> を含む <see cref="CallToolResult"/>。
    /// アクティブが無い場合は <c>NO_ACTIVE_SESSION</c>、session 不明は <c>NOT_FOUND</c>、kill 失敗は <c>KILL_FAILED</c>。
    /// </returns>
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

    /// <summary>
    /// 現在 attach 中のすべての session に対して close を試行し、結果を session 単位の配列として返す。
    /// </summary>
    /// <param name="ct">キャンセル トークン。<see cref="OperationCanceledException"/> はそのまま伝播する。</param>
    /// <returns>
    /// <c>{ results: [ { sessionId, result: "ok"|"fail", error?, message? }, ... ], hasFailures }</c> を含む <see cref="CallToolResult"/>。
    /// <see cref="OperationCanceledException"/> はそのまま伝播し、それ以外の個別失敗は配列要素に結果化される。
    /// </returns>
    /// <remarks>キャンセル以外の例外は session 単位で結果化し、残り session への close 試行を継続する。</remarks>
    [McpServerTool(Name = "windows_close_all")]
    [Description("Close every attached window. Returns a per-session result array. Partial failure is reported, not thrown.")]
    public async Task<CallToolResult> CloseAllAsync(CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        var snapshot = _store.ListAll();
        var results = new JsonArray();
        var hasFailures = false;

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
            catch (Exception ex)
            {
                hasFailures = true;
                entry["result"] = "fail";
                entry["error"] = GetToolErrorCode(ex) ?? ToolErrors.InternalError;
                entry["message"] = ex.Message;

                if (ex is CloseFailedException)
                {
                    _logger.LogDebug(ex, "windows_close_all: closing session {Sid} failed", sid);
                }
                else
                {
                    _logger.LogError(ex, "windows_close_all: closing session {Sid} failed unexpectedly", sid);
                }
            }

            results.Add(entry);
        }

        return SuccessJson(new JsonObject
        {
            ["results"] = results,
            ["hasFailures"] = hasFailures,
        });
    }

    /// <summary>
    /// 全 session を detach した後、HTTP daemon の graceful shutdown を要求する。stdio モードでは未対応。
    /// </summary>
    /// <param name="ct">キャンセル トークン。</param>
    /// <returns>
    /// 成功時は <c>{ stopped: true }</c> を含む <see cref="CallToolResult"/>。
    /// stdio モードでは <c>LOCAL_ONLY</c>、停止処理が失敗した場合は <c>INTERNAL_ERROR</c> を返す。
    /// </returns>
    /// <remarks>設計 §4.5 に従い、ここでは window を close せず session 記録のみ解放する。</remarks>
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

    /// <summary>
    /// <paramref name="sessionId"/> が <c>null</c> の場合はアクティブ session を採用し、無ければ <c>NO_ACTIVE_SESSION</c> エラーを構築する。
    /// </summary>
    /// <param name="sessionId">呼び出し元から渡された sessionId。省略時は <c>null</c>。</param>
    /// <param name="resolvedId">解決された sessionId。失敗時は <see cref="string.Empty"/>。</param>
    /// <param name="error">解決失敗時のエラー結果。成功時は <c>null</c>。</param>
    /// <returns>解決に成功したかどうか。</returns>
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
    /// <paramref name="sessionId"/> に対応する <see cref="WindowRefStore"/> エントリの sessionId 紐付けを解除し、
    /// <paramref name="session"/> を Dispose する。Dispose / Clear で発生した例外は握りつぶしてデバッグログに記録する。
    /// </summary>
    /// <param name="sessionId">既に <see cref="SessionStore"/> から取り外された session の ID。</param>
    /// <param name="session">Dispose 対象の <see cref="IWindowSession"/>。</param>
    private void DetachSession(string sessionId, IWindowSession session)
    {
        try { _refStore.RemoveBySessionId(sessionId); }
        catch (Exception ex) { _logger.LogDebug(ex, "RemoveBySessionId failed for {SessionId}", sessionId); }
        try { session.Dispose(); }
        catch (Exception ex) { _logger.LogDebug(ex, "Disposing session {Sid} failed", sessionId); }
    }

    /// <summary>
    /// 任意の JSON object を text content (シリアライズ済み) と structured content の両方に載せた成功レスポンスを構築する。
    /// </summary>
    /// <param name="obj">レスポンス本体となる JSON object。</param>
    /// <returns><see cref="CallToolResult.IsError"/> = <c>false</c> の <see cref="CallToolResult"/>。</returns>
    private static CallToolResult SuccessJson(JsonObject obj)
    {
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = obj.ToJsonString() }],
            StructuredContent = JsonSerializer.SerializeToElement(obj),
        };
    }
}
