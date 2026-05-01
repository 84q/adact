using System.ComponentModel;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    /// <summary>アタッチ済みウィンドウのサイズを変更する (UIA TransformPattern.Resize)。</summary>
    /// <param name="width">新しい幅 (px、>0)。</param>
    /// <param name="height">新しい高さ (px、>0)。</param>
    /// <param name="sessionId">対象 session。省略時はアクティブ session。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。Pattern 不在 / 操作失敗は <c>ELEMENT_INTERACTION_FAILED</c>。</returns>
    [McpServerTool(Name = "windows_resize")]
    [Description("Resize the attached window via UIA TransformPattern.Resize. Errors with ELEMENT_INTERACTION_FAILED if the window does not support resize.")]
    public async Task<CallToolResult> ResizeAsync(
        [Description("New window width in pixels (must be > 0).")]
        int width,
        [Description("New window height in pixels (must be > 0).")]
        int height,
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (width <= 0)
            return ToolErrors.Error(ToolErrors.InvalidArgument, "width must be > 0.");
        if (height <= 0)
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
        catch (Exception ex) { return MapOrLog(ex, "windows_resize"); }
    }

    /// <summary>アタッチ済みウィンドウを最小化する (UIA WindowPattern.SetWindowVisualState)。</summary>
    /// <param name="sessionId">対象 session。省略時はアクティブ session。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。Pattern 不在は <c>ELEMENT_INTERACTION_FAILED</c>。</returns>
    [McpServerTool(Name = "windows_minimize")]
    [Description("Minimize the attached window via UIA WindowPattern.SetWindowVisualState(Minimized).")]
    public async Task<CallToolResult> MinimizeAsync(
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
        => await InvokeWindowStateAsync("windows_minimize", sessionId, s => s.MinimizeAsync(ct), ct).ConfigureAwait(false);

    /// <summary>アタッチ済みウィンドウを最大化する (UIA WindowPattern.SetWindowVisualState)。</summary>
    /// <param name="sessionId">対象 session。省略時はアクティブ session。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。Pattern 不在は <c>ELEMENT_INTERACTION_FAILED</c>。</returns>
    [McpServerTool(Name = "windows_maximize")]
    [Description("Maximize the attached window via UIA WindowPattern.SetWindowVisualState(Maximized).")]
    public async Task<CallToolResult> MaximizeAsync(
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
        => await InvokeWindowStateAsync("windows_maximize", sessionId, s => s.MaximizeAsync(ct), ct).ConfigureAwait(false);

    /// <summary>アタッチ済みウィンドウを通常表示に復元する (UIA WindowPattern.SetWindowVisualState)。</summary>
    /// <param name="sessionId">対象 session。省略時はアクティブ session。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。Pattern 不在は <c>ELEMENT_INTERACTION_FAILED</c>。</returns>
    [McpServerTool(Name = "windows_restore")]
    [Description("Restore the attached window to normal state via UIA WindowPattern.SetWindowVisualState(Normal).")]
    public async Task<CallToolResult> RestoreAsync(
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
        => await InvokeWindowStateAsync("windows_restore", sessionId, s => s.RestoreAsync(ct), ct).ConfigureAwait(false);

    /// <summary>
    /// minimize / maximize / restore の共通実装。session 解決とエラーマッピングをまとめる。
    /// </summary>
    /// <param name="toolName">MCP ツール名 (ログ用)。</param>
    /// <param name="sessionId">対象 session。null はアクティブ。</param>
    /// <param name="op">解決済み <see cref="Adact.Engine.IWindowSession"/> に対する操作。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content、業務例外は <c>CallToolResult</c>、未マップ例外は再 throw。</returns>
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
