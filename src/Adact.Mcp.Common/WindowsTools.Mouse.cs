using System.ComponentModel;

using Adact.Engine;
using Adact.Engine.Snapshot;

using Microsoft.Extensions.Logging;

using FlaUiMouseButton = FlaUI.Core.Input.MouseButton;

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

    /// <summary>マウスカーソルを絶対座標 (x,y) に移動する。</summary>
    /// <param name="target">"x,y"。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_mouse_move")]
    [Description("Move the mouse cursor to absolute screen coordinates 'x,y'. This is a low-level global input operation and does not require a session.")]
    public async Task<CallToolResult> MouseMoveAsync(
        [Description("Absolute screen coordinates ('x,y').")]
        string target,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!TryParsePointTarget(target, out var point, out var error)) return error!;
        try
        {
            FlaUI.Core.Input.Mouse.MoveTo(point.X, point.Y);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_mouse_move"); }
    }

    /// <summary>現在カーソル位置でマウスボタンを押下したまま保持する。</summary>
    /// <param name="button">"left" (default) / "right" / "middle"。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_mouse_down")]
    [Description("Press and hold a mouse button at the current cursor position. Pair with windows_mouse_up to release. This is a low-level global input operation and does not require a session.")]
    public async Task<CallToolResult> MouseDownAsync(
        [Description("Mouse button: 'left' (default), 'right', or 'middle'.")]
        string? button = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!TryParseMouseButton(button, out var btn, out var btnError))
            return ToolErrors.Error(ToolErrors.InvalidArgument, btnError);
        try
        {
            FlaUI.Core.Input.Mouse.Down(btn switch
            {
                Adact.Engine.MouseButton.Right => FlaUiMouseButton.Right,
                Adact.Engine.MouseButton.Middle => FlaUiMouseButton.Middle,
                _ => FlaUiMouseButton.Left,
            });
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_mouse_down"); }
    }

    /// <summary>現在カーソル位置でマウスボタンを解放する。</summary>
    /// <param name="button">"left" (default) / "right" / "middle"。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_mouse_up")]
    [Description("Release a mouse button at the current cursor position. Pair with windows_mouse_down. This is a low-level global input operation and does not require a session.")]
    public async Task<CallToolResult> MouseUpAsync(
        [Description("Mouse button: 'left' (default), 'right', or 'middle'.")]
        string? button = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!TryParseMouseButton(button, out var btn, out var btnError))
            return ToolErrors.Error(ToolErrors.InvalidArgument, btnError);
        try
        {
            FlaUI.Core.Input.Mouse.Up(btn switch
            {
                Adact.Engine.MouseButton.Right => FlaUiMouseButton.Right,
                Adact.Engine.MouseButton.Middle => FlaUiMouseButton.Middle,
                _ => FlaUiMouseButton.Left,
            });
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_mouse_up"); }
    }

    /// <summary>現在カーソル位置でマウスホイールをスクロールする。<paramref name="deltaY"/> 正値=下、<paramref name="deltaX"/> 正値=右。</summary>
    /// <param name="deltaY">垂直スクロール量 (notch)。正値で下方向。</param>
    /// <param name="deltaX">水平スクロール量 (notch)。正値で右方向。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_mouse_wheel")]
    [Description("Scroll the mouse wheel at the current cursor position. deltaY > 0 scrolls down, deltaX > 0 scrolls right. This is a low-level global input operation and does not require a session.")]
    public async Task<CallToolResult> MouseWheelAsync(
        [Description("Vertical scroll amount in notches (positive = down).")]
        int deltaY = 0,
        [Description("Horizontal scroll amount in notches (positive = right).")]
        int deltaX = 0,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        try
        {
            if (deltaY != 0)
            {
                FlaUI.Core.Input.Mouse.Scroll(-deltaY);
            }
            if (deltaX != 0)
            {
                FlaUI.Core.Input.Mouse.HorizontalScroll(deltaX);
            }
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

    /// <summary>target を <c>x,y</c> 座標として解析する。</summary>
    /// <param name="target">入力文字列。</param>
    /// <param name="point">解析結果。</param>
    /// <param name="error">エラー結果。</param>
    /// <returns>成功時 true。</returns>
    private static bool TryParsePointTarget(string target, out MouseTarget.ByPoint point, out CallToolResult? error)
    {
        point = null!;
        if (string.IsNullOrEmpty(target))
        {
            error = ToolErrors.Error(ToolErrors.InvalidArgument, "target must be a non-empty string.");
            return false;
        }

        try
        {
            var parsed = MouseTarget.Parse(target);
            if (parsed is not MouseTarget.ByPoint byPoint)
            {
                error = ToolErrors.Error(ToolErrors.InvalidArgument,
                    "target must be absolute screen coordinates in 'x,y' form.");
                return false;
            }
            point = byPoint;
        }
        catch (ArgumentException ex)
        {
            error = ToolErrors.Error(ToolErrors.InvalidArgument, ex.Message);
            return false;
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
