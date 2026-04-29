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
    /// 指定 Element Ref が指す UIA 要素の詳細プロパティを返す (設計 022 §8)。auto-snapshot は発火しない。
    /// </summary>
    /// <param name="ref">snapshot 由来の element ref (例: <c>s1e7</c>)。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>UIA プロパティと対応 Pattern の状態を JSON で返す <see cref="CallToolResult"/>。</returns>
    [McpServerTool(Name = "windows_inspect")]
    [Description("Get detailed UIA properties (Name, ControlType, AutomationId, ClassName, HelpText, Value, BoundingRect, state flags, supported patterns) of the element identified by ref. No snapshot is taken.")]
    public async Task<CallToolResult> InspectAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent windows_snapshot.")]
        string @ref,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (!ValidateRef(@ref, out var session, out var error))
            return error!;

        try
        {
            var result = await session!.InspectAsync(@ref, ct).ConfigureAwait(false);
            var json = SerializeInspectResult(result);
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = json.ToJsonString() }],
                StructuredContent = JsonSerializer.SerializeToElement(json),
            };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_inspect"); }
    }

    /// <summary>
    /// <see cref="InspectResult"/> を MCP 返却用 JSON オブジェクトに変換する。設計 022 §8 のスキーマに従う。
    /// </summary>
    /// <param name="r">Engine から得た inspect 結果。</param>
    /// <returns>JSON オブジェクト。</returns>
    private static JsonObject SerializeInspectResult(InspectResult r)
    {
        var patterns = new JsonObject();
        foreach (var (key, body) in r.Patterns)
        {
            var inner = new JsonObject();
            foreach (var (k, v) in body)
            {
                inner[k] = v switch
                {
                    null => null,
                    bool b => b,
                    string s => s,
                    int i => i,
                    double d => d,
                    _ => v.ToString(),
                };
            }
            patterns[key] = inner;
        }
        return new JsonObject
        {
            ["ref"] = r.Ref,
            ["name"] = r.Name,
            ["controlType"] = r.ControlType,
            ["automationId"] = r.AutomationId,
            ["className"] = r.ClassName,
            ["helpText"] = r.HelpText,
            ["value"] = r.Value,
            ["boundingRect"] = new JsonObject
            {
                ["x"] = r.BoundingRect.X,
                ["y"] = r.BoundingRect.Y,
                ["width"] = r.BoundingRect.Width,
                ["height"] = r.BoundingRect.Height,
            },
            ["isEnabled"] = r.IsEnabled,
            ["isOffscreen"] = r.IsOffscreen,
            ["isKeyboardFocusable"] = r.IsKeyboardFocusable,
            ["hasKeyboardFocus"] = r.HasKeyboardFocus,
            ["patterns"] = patterns,
        };
    }
}
