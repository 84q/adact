using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    /// <summary>
    /// </summary>
    [McpServerTool(Name = "adact_inspect")]
    [Description("Get detailed UIA properties (Name, ControlType, AutomationId, ClassName, HelpText, Value, BoundingRect, state flags, supported patterns) of the element identified by ref. No snapshot is taken.")]
    public async Task<CallToolResult> InspectAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent adact_snapshot.")]
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
        catch (Exception ex) { return MapOrLog(ex, "adact_inspect"); }
    }

    /// <summary>
    /// </summary>
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
                    string[] arr => new JsonArray(arr.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray()),
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
            ["selector"] = r.Selector is { } sel ? new JsonObject
            {
                ["stability"] = sel.Stability,
                ["code"] = sel.Code,
            } : null,
        };
    }
}
