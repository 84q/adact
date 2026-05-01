using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    /// <summary>
    /// アタッチ済みウィンドウまたは指定要素の bounding rect を PNG として保存する (設計 022 §10)。
    /// auto-snapshot は発火しない。
    /// </summary>
    /// <param name="ref">クリップ対象の Element Ref。null/省略でウィンドウ全体。</param>
    /// <param name="out">出力ファイルパス。null/省略で <c>.adact/screenshot-&lt;sid&gt;-&lt;ts&gt;.png</c>。</param>
    /// <param name="sessionId"><paramref name="ref"/> 未指定時のみ参照される対象 session。null はアクティブ。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>保存先 / 幅 / 高さを JSON で返す <see cref="CallToolResult"/>。</returns>
    [McpServerTool(Name = "windows_screenshot")]
    [Description("Capture a PNG screenshot of the attached window (or a specific element when ref is provided) and save it to disk. No snapshot is taken.")]
    public async Task<CallToolResult> ScreenshotAsync(
        [Description("Element ref to clip. Omit to capture the whole attached window.")]
        string? @ref = null,
        [Description("Output PNG path. Omit to save under '.adact/screenshot-<sid>-<UTC ts>.png'.")]
        string? @out = null,
        [Description("Session ID like 's1' (used only when ref is omitted). Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(@out)
            && !string.Equals(Path.GetExtension(@out), ".png", StringComparison.OrdinalIgnoreCase))
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                $"'out' must end with '.png' (got '{@out}'). Screenshot format is PNG-only.");
        }

        IWindowSession? session;
        if (!string.IsNullOrEmpty(@ref))
        {
            if (!ValidateRef(@ref, out session, out var refError))
                return refError!;
        }
        else
        {
            if (!TryResolveSessionId(sessionId, out var sid, out var sessError))
                return sessError!;
            if (!_store.TryGet(sid, out session))
                return ToolErrors.Error(ToolErrors.NotFound, $"Session '{sid}' not found.");
        }

        try
        {
            var result = await session!.ScreenshotAsync(string.IsNullOrEmpty(@ref) ? null : @ref, @out, ct).ConfigureAwait(false);
            var json = new JsonObject
            {
                ["path"] = result.Path,
                ["width"] = result.Width,
                ["height"] = result.Height,
            };
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = json.ToJsonString() }],
                StructuredContent = JsonSerializer.SerializeToElement(json),
            };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_screenshot"); }
    }
}
