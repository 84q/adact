using System.ComponentModel;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    /// <summary>Resizes the attached window via UIA TransformPattern.Resize. When only one dimension is provided, the other keeps its current value.</summary>
    /// <param name="width">New width in pixels (must be &gt; 0). Omit to keep the current width.</param>
    /// <param name="height">New height in pixels (must be &gt; 0). Omit to keep the current height.</param>
    /// <param name="sessionId">Target session. Omit to use the active session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Empty content on success. Pattern missing / interaction failure maps to <c>ELEMENT_INTERACTION_FAILED</c>.</returns>
    [McpServerTool(Name = "adact_resize_window")]
    [Description("Resize the attached window via UIA TransformPattern.Resize. Provide at least one of width/height. Omitted dimension keeps its current value.")]
    public async Task<CallToolResult> ResizeAsync(
        [Description("New window width in pixels (must be > 0). Omit to keep current width.")]
        int? width = null,
        [Description("New window height in pixels (must be > 0). Omit to keep current height.")]
        int? height = null,
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (width is null && height is null)
            return ToolErrors.Error(ToolErrors.InvalidArgument, "At least one of 'width' or 'height' must be specified.");
        if (width is <= 0)
            return ToolErrors.Error(ToolErrors.InvalidArgument, "width must be > 0.");
        if (height is <= 0)
            return ToolErrors.Error(ToolErrors.InvalidArgument, "height must be > 0.");

        if (!TryResolveSessionId(sessionId, out var sid, out var error))
            return error!;
        if (!_store.TryGet(sid, out var session))
            return ToolErrors.Error(ToolErrors.NotFound, $"Session '{sid}' not found.");

        try
        {
            await session.ResizeAsync(width, height, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_resize_window"); }
    }

    /// <summary>Minimizes the attached window via UIA WindowPattern.SetWindowVisualState.</summary>
    /// <param name="sessionId">Target session. Omit to use the active session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Empty content on success. Pattern missing maps to <c>ELEMENT_INTERACTION_FAILED</c>.</returns>
    [McpServerTool(Name = "adact_minimize_window")]
    [Description("Minimize the attached window via UIA WindowPattern.SetWindowVisualState(Minimized).")]
    public async Task<CallToolResult> MinimizeAsync(
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
        => await InvokeWindowStateAsync("adact_minimize_window", sessionId, s => s.MinimizeAsync(ct), ct).ConfigureAwait(false);

    /// <summary>Maximizes the attached window via UIA WindowPattern.SetWindowVisualState.</summary>
    /// <param name="sessionId">Target session. Omit to use the active session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Empty content on success. Pattern missing maps to <c>ELEMENT_INTERACTION_FAILED</c>.</returns>
    [McpServerTool(Name = "adact_maximize_window")]
    [Description("Maximize the attached window via UIA WindowPattern.SetWindowVisualState(Maximized).")]
    public async Task<CallToolResult> MaximizeAsync(
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
        => await InvokeWindowStateAsync("adact_maximize_window", sessionId, s => s.MaximizeAsync(ct), ct).ConfigureAwait(false);

    /// <summary>Restores the attached window to its normal state via UIA WindowPattern.SetWindowVisualState.</summary>
    /// <param name="sessionId">Target session. Omit to use the active session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Empty content on success. Pattern missing maps to <c>ELEMENT_INTERACTION_FAILED</c>.</returns>
    [McpServerTool(Name = "adact_restore_window")]
    [Description("Restore the attached window to normal state via UIA WindowPattern.SetWindowVisualState(Normal).")]
    public async Task<CallToolResult> RestoreAsync(
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
        => await InvokeWindowStateAsync("adact_restore_window", sessionId, s => s.RestoreAsync(ct), ct).ConfigureAwait(false);

    /// <summary>
    /// Shared implementation for minimize / maximize / restore. Resolves the session and maps errors.
    /// </summary>
    /// <param name="toolName">MCP tool name (used for logging).</param>
    /// <param name="sessionId">Target session. Null means the active session.</param>
    /// <param name="op">Operation to run against the resolved <see cref="Adact.Engine.IWindowSession"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Empty content on success, a <c>CallToolResult</c> for handled errors, or throws on unmapped exceptions.</returns>
    private async Task<CallToolResult> InvokeWindowStateAsync(
        string toolName,
        string? sessionId,
        Func<Adact.Engine.IWindowSession, Task> op,
        CancellationToken ct)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (!TryResolveSessionId(sessionId, out var sid, out var error))
            return error!;
        if (!_store.TryGet(sid, out var session))
            return ToolErrors.Error(ToolErrors.NotFound, $"Session '{sid}' not found.");

        try
        {
            await op(session).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, toolName); }
    }
}
