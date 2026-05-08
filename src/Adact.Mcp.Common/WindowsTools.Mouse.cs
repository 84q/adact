using System.ComponentModel;

using Adact.Engine;
using Adact.Engine.Snapshot;
using Adact.Mcp.Common.InputDrivers;

using Microsoft.Extensions.Logging;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    /// <param name="button">"left" (default) / "right" / "middle"。</param>
    [McpServerTool(Name = "adact_doubleclick")]
    [Description("Double-click an element identified by ref. The session is determined automatically from the ref id prefix.")]
    public async Task<CallToolResult> DblclickAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent adact_snapshot.")]
        string @ref,
        [Description("Mouse button: 'left' (default), 'right', or 'middle'.")]
        string? button = null,
        [Description("Modifier keys held during the click. Allowed: 'Shift', 'Control', 'Ctrl', 'Alt', 'Meta', 'Win', 'Windows'.")]
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
        catch (Exception ex) { return MapOrLog(ex, "adact_doubleclick"); }
    }

    [McpServerTool(Name = "adact_hover")]
    [Description("Move the mouse cursor over an element identified by ref. The session is determined automatically from the ref id prefix.")]
    public async Task<CallToolResult> HoverAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent adact_snapshot.")]
        string @ref,
        [Description("Modifier keys held during hover. Allowed: 'Shift', 'Control', 'Ctrl', 'Alt', 'Meta', 'Win', 'Windows'.")]
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
        catch (Exception ex) { return MapOrLog(ex, "adact_hover"); }
    }

    /// <param name="target">"x,y"。</param>
    [McpServerTool(Name = "adact_mousemove")]
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
            _mouseDriver.MoveTo(point.X, point.Y);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_mousemove"); }
    }

    /// <param name="button">"left" (default) / "right" / "middle"。</param>
    [McpServerTool(Name = "adact_mousedown")]
    [Description("Press and hold a mouse button at the current cursor position. Pair with adact_mouseup to release. This is a low-level global input operation and does not require a session.")]
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
            _mouseDriver.Down(btn);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_mousedown"); }
    }

    /// <param name="button">"left" (default) / "right" / "middle"。</param>
    [McpServerTool(Name = "adact_mouseup")]
    [Description("Release a mouse button at the current cursor position. Pair with adact_mousedown. This is a low-level global input operation and does not require a session.")]
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
            _mouseDriver.Up(btn);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_mouseup"); }
    }

    [McpServerTool(Name = "adact_mousewheel")]
    [Description("Scroll the mouse wheel at the current cursor position. deltaY > 0 scrolls down, deltaX > 0 scrolls right. This is a low-level global input operation and does not require a session.")]
    public async Task<CallToolResult> MouseWheelAsync(
        [Description("Vertical scroll amount in notches (positive = down).")]
        int deltaY = 0,
        [Description("Horizontal scroll amount in notches (positive = right).")]
        int deltaX = 0,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (deltaX == 0 && deltaY == 0)
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "At least one of deltaX or deltaY must be non-zero.");
        try
        {
            if (deltaY != 0)
            {
                _mouseDriver.Scroll(-deltaY);
            }
            if (deltaX != 0)
            {
                _mouseDriver.HorizontalScroll(deltaX);
            }
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_mousewheel"); }
    }

    /// <param name="ref">element ref。</param>
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

    private CallToolResult MapOrLog(Exception ex, string toolName)
    {
        var mapped = ToolErrors.TryMap(ex);
        if (mapped is not null) return mapped;
        _logger.LogError(ex, "{Tool} failed unexpectedly", toolName);
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
        return null!; // unreachable
    }
}
