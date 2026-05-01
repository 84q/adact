using System.ComponentModel;

using Adact.Engine;
using Adact.Engine.Snapshot;

using Microsoft.Extensions.Logging;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    /// <summary>指定要素をダブルクリックする。修飾キー / 位置 / ボタンは任意。</summary>
    /// <param name="ref">snapshot 由来の element ref。</param>
    /// <param name="button">"left" (default) / "right" / "middle"。</param>
    /// <param name="modifiers">押下する修飾キー名 (Shift/Control/Alt/Meta/ControlOrMeta)。</param>
    /// <param name="positionX">要素左上基準 X オフセット (px)。</param>
    /// <param name="positionY">要素左上基準 Y オフセット (px)。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。引数不正・解決失敗・操作失敗は対応する error コード。</returns>
    [McpServerTool(Name = "windows_dblclick")]
    [Description("Double-click an element identified by ref. The session is determined automatically from the ref id prefix.")]
    public async Task<CallToolResult> DblclickAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent windows_snapshot.")]
        string @ref,
        [Description("Mouse button: 'left' (default), 'right', or 'middle'.")]
        string? button = null,
        [Description("Modifier keys held during the click. Allowed: 'Shift', 'Control', 'Ctrl', 'Alt', 'Meta', 'ControlOrMeta'.")]
        IReadOnlyList<string>? modifiers = null,
        [Description("X offset (px) from the element's bounding-rectangle top-left. Omit to click center.")]
        int? positionX = null,
        [Description("Y offset (px) from the element's bounding-rectangle top-left. Omit to click center.")]
        int? positionY = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var refError)) return refError!;
        if (!TryParseMouseButton(button, out var btn, out var btnError))
            return ToolErrors.Error(ToolErrors.InvalidArgument, btnError);

        try
        {
            var opts = new ClickOptions(
                Double: true,
                Button: btn,
                Count: 1,
                Modifiers: modifiers,
                PositionX: positionX,
                PositionY: positionY);
            await session!.DoubleClickAsync(@ref, opts, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_dblclick"); }
    }

    /// <summary>指定要素にマウスカーソルを移動 (hover) する。</summary>
    /// <param name="ref">snapshot 由来の element ref。</param>
    /// <param name="modifiers">押下する修飾キー名。</param>
    /// <param name="positionX">要素左上基準 X オフセット (px)。</param>
    /// <param name="positionY">要素左上基準 Y オフセット (px)。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_hover")]
    [Description("Move the mouse cursor over an element identified by ref. The session is determined automatically from the ref id prefix.")]
    public async Task<CallToolResult> HoverAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent windows_snapshot.")]
        string @ref,
        [Description("Modifier keys held during hover. Allowed: 'Shift', 'Control', 'Ctrl', 'Alt', 'Meta', 'ControlOrMeta'.")]
        IReadOnlyList<string>? modifiers = null,
        [Description("X offset (px) from the element's bounding-rectangle top-left. Omit to hover the center.")]
        int? positionX = null,
        [Description("Y offset (px) from the element's bounding-rectangle top-left. Omit to hover the center.")]
        int? positionY = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var refError)) return refError!;
        try
        {
            await session!.HoverAsync(@ref, modifiers, positionX, positionY, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_hover"); }
    }

    /// <summary>マウスカーソルを target (要素 ref または "x,y") に移動する。</summary>
    /// <param name="target">"s&lt;sid&gt;e&lt;eid&gt;" または "x,y"。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_mouse_move")]
    [Description("Move the mouse cursor to a target (element ref or absolute screen coordinates 'x,y').")]
    public async Task<CallToolResult> MouseMoveAsync(
        [Description("Either an element ref ('s<sid>e<eid>') or screen coordinates ('x,y').")]
        string target,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateMouseTarget(target, out var mt, out var session, out var error)) return error!;
        try
        {
            await session!.MouseMoveAsync(mt!, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_mouse_move"); }
    }

    /// <summary>target の位置でマウスボタンを押下したまま保持する。</summary>
    /// <param name="target">"s&lt;sid&gt;e&lt;eid&gt;" または "x,y"。</param>
    /// <param name="button">"left" (default) / "right" / "middle"。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_mouse_down")]
    [Description("Press and hold a mouse button at the target. Pair with windows_mouse_up to release.")]
    public async Task<CallToolResult> MouseDownAsync(
        [Description("Either an element ref ('s<sid>e<eid>') or screen coordinates ('x,y').")]
        string target,
        [Description("Mouse button: 'left' (default), 'right', or 'middle'.")]
        string? button = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateMouseTarget(target, out var mt, out var session, out var error)) return error!;
        if (!TryParseMouseButton(button, out var btn, out var btnError))
            return ToolErrors.Error(ToolErrors.InvalidArgument, btnError);
        try
        {
            await session!.MouseDownAsync(mt!, btn, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_mouse_down"); }
    }

    /// <summary>target の位置でマウスボタンを解放する。</summary>
    /// <param name="target">"s&lt;sid&gt;e&lt;eid&gt;" または "x,y"。</param>
    /// <param name="button">"left" (default) / "right" / "middle"。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_mouse_up")]
    [Description("Release a mouse button at the target. Pair with windows_mouse_down.")]
    public async Task<CallToolResult> MouseUpAsync(
        [Description("Either an element ref ('s<sid>e<eid>') or screen coordinates ('x,y').")]
        string target,
        [Description("Mouse button: 'left' (default), 'right', or 'middle'.")]
        string? button = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateMouseTarget(target, out var mt, out var session, out var error)) return error!;
        if (!TryParseMouseButton(button, out var btn, out var btnError))
            return ToolErrors.Error(ToolErrors.InvalidArgument, btnError);
        try
        {
            await session!.MouseUpAsync(mt!, btn, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_mouse_up"); }
    }

    /// <summary>target 位置でマウスホイールをスクロールする。<paramref name="deltaY"/> 正値=下、<paramref name="deltaX"/> 正値=右。</summary>
    /// <param name="target">"s&lt;sid&gt;e&lt;eid&gt;" または "x,y"。</param>
    /// <param name="deltaY">垂直スクロール量 (notch)。正値で下方向。</param>
    /// <param name="deltaX">水平スクロール量 (notch)。正値で右方向。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_mouse_wheel")]
    [Description("Scroll the mouse wheel at the target. deltaY > 0 scrolls down, deltaX > 0 scrolls right.")]
    public async Task<CallToolResult> MouseWheelAsync(
        [Description("Either an element ref ('s<sid>e<eid>') or screen coordinates ('x,y').")]
        string target,
        [Description("Vertical scroll amount in notches (positive = down).")]
        int deltaY = 0,
        [Description("Horizontal scroll amount in notches (positive = right).")]
        int deltaX = 0,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateMouseTarget(target, out var mt, out var session, out var error)) return error!;
        try
        {
            await session!.MouseWheelAsync(mt!, deltaX, deltaY, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_mouse_wheel"); }
    }

    /// <summary>"s..e.." 形式の ref を解析し、対応する session を取得する共通ヘルパ。</summary>
    /// <param name="ref">element ref。</param>
    /// <param name="session">解決された session (失敗時は null)。</param>
    /// <param name="error">エラー結果 (成功時は null)。</param>
    /// <returns>成功時 true。</returns>
    private bool ValidateRef(string @ref, out IWindowSession? session, out CallToolResult? error)
    {
        session = null;
        if (string.IsNullOrEmpty(@ref))
        {
            error = ToolErrors.Error(ToolErrors.InvalidArgument, "ref must be a non-empty string.");
            return false;
        }
        if (!RefId.TryParse(@ref, out _, out _))
        {
            error = ToolErrors.Error(ToolErrors.RefNotFound, $"Ref ID '{@ref}' is malformed.");
            return false;
        }
        session = _store.ResolveByRef(@ref);
        if (session is null)
        {
            error = ToolErrors.Error(ToolErrors.RefNotFound,
                $"Ref ID '{@ref}' does not match any known session.");
            return false;
        }
        error = null;
        return true;
    }

    /// <summary>target を <see cref="MouseTarget"/> に解析し、ByRef なら ref から、ByPoint なら active session を取得する。</summary>
    /// <param name="target">入力文字列。</param>
    /// <param name="parsed">解析結果。</param>
    /// <param name="session">対応 session。</param>
    /// <param name="error">エラー結果。</param>
    /// <returns>成功時 true。</returns>
    private bool ValidateMouseTarget(string target, out MouseTarget? parsed, out IWindowSession? session, out CallToolResult? error)
    {
        parsed = null;
        session = null;
        if (string.IsNullOrEmpty(target))
        {
            error = ToolErrors.Error(ToolErrors.InvalidArgument, "target must be a non-empty string.");
            return false;
        }

        try
        {
            parsed = MouseTarget.Parse(target);
        }
        catch (ArgumentException ex)
        {
            error = ToolErrors.Error(ToolErrors.InvalidArgument, ex.Message);
            return false;
        }

        if (parsed is MouseTarget.ByRef byRef)
        {
            session = _store.ResolveByRef(byRef.Ref);
            if (session is null)
            {
                error = ToolErrors.Error(ToolErrors.RefNotFound,
                    $"Ref ID '{byRef.Ref}' does not match any known session.");
                return false;
            }
        }
        else
        {
            session = _store.GetActiveOrNull();
            if (session is null)
            {
                error = ToolErrors.Error(ToolErrors.NoActiveSession,
                    "No active session: attach to a window first or use a ref-based target.");
                return false;
            }
        }
        error = null;
        return true;
    }

    /// <summary>業務例外を <see cref="ToolErrors.TryMap"/> でマップし、未マップ例外はログ出力後再 throw する。</summary>
    /// <param name="ex">捕捉した例外。</param>
    /// <param name="toolName">ログ用ツール名。</param>
    /// <returns>業務例外の <see cref="CallToolResult"/>。未マップは元のスタックで再 throw する。</returns>
    private CallToolResult MapOrLog(Exception ex, string toolName)
    {
        var mapped = ToolErrors.TryMap(ex);
        if (mapped is not null) return mapped;
        _logger.LogError(ex, "{Tool} failed unexpectedly", toolName);
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
        return null!; // unreachable
    }
}
