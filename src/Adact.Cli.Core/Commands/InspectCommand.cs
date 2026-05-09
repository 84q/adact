using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class InspectCommand
{
    public static Command Build()
    {
        var refArg = new Argument<string>("ref")
        {
            Description = "Element Ref ID like 's1e7'.",
        };

        var cmd = new Command("inspect", "Print detailed UIA properties of the element identified by ref as a single JSON line.");
        cmd.Arguments.Add(refArg);

        cmd.SetAction((parseResult, ct) =>
        {
            var refValue = parseResult.GetValue(refArg);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            if (!RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => ExecuteAsync(client, refValue!, token),
                ct);
        });

        return cmd;
    }

    /// <param name="ct">cancellation token。</param>
    /// <param name="client">The connected MCP client.</param>
    /// <param name="refValue">The element reference to inspect.</param>
    /// <returns>exit code。</returns>
    private static async Task<int> ExecuteAsync(IAdactMcpClient client, string refValue, CancellationToken ct)
    {
        var args = new Dictionary<string, object?> { ["ref"] = refValue };
        var result = await client.CallToolAsync("adact_inspect", args, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var json = McpResponse.GetJson(result);
        var excludeIfEmpty = new HashSet<string> { "name", "automationId", "className", "helpText", "value" };
        var fields = CliOutput.JsonObjectToFields(json, "ref", "name", "controlType", "automationId", "className", "helpText", "value", "boundingRect", "isEnabled", "isOffscreen", "isKeyboardFocusable", "hasKeyboardFocus")
            .Where(kv => kv.Key != "patterns" && kv.Key != "selector" && kv.Key != "boundingRect"
                && kv.Key != "isEnabled" && kv.Key != "isOffscreen" && kv.Key != "isKeyboardFocusable" && kv.Key != "hasKeyboardFocus"
                && !(excludeIfEmpty.Contains(kv.Key) && string.IsNullOrEmpty(kv.Value)))
            .ToList();
        CliOutput.WriteYamlSuccess(metaFields: null, fields);

        if (json.TryGetProperty("boundingRect", out var boundingRect) && boundingRect.ValueKind == JsonValueKind.Object)
        {
            FormatBoundingRect(boundingRect);
        }

        FormatState(json);

        if (json.TryGetProperty("patterns", out var patterns) && patterns.ValueKind == JsonValueKind.Object)
        {
            FormatPatterns(patterns);
        }

        if (json.TryGetProperty("selector", out var selector) && selector.ValueKind == JsonValueKind.Object)
        {
            FormatSelector(selector);
        }

        return ExitCodes.Success;
    }

    private static void FormatBoundingRect(JsonElement rect)
    {
        var x = rect.TryGetProperty("x", out var xv) ? xv.GetRawText() : "0";
        var y = rect.TryGetProperty("y", out var yv) ? yv.GetRawText() : "0";
        var width = rect.TryGetProperty("width", out var wv) ? wv.GetRawText() : "0";
        var height = rect.TryGetProperty("height", out var hv) ? hv.GetRawText() : "0";
        Console.Out.WriteLine($"boundingRect: {{x: {x}, y: {y}, width: {width}, height: {height}}}");
    }

    private static void FormatState(JsonElement json)
    {
        var keywords = new List<string>();
        if (json.TryGetProperty("isEnabled", out var e) && e.ValueKind == JsonValueKind.True)
            keywords.Add("enabled");
        if (json.TryGetProperty("isOffscreen", out var o) && o.ValueKind == JsonValueKind.True)
            keywords.Add("offscreen");
        if (json.TryGetProperty("isKeyboardFocusable", out var kf) && kf.ValueKind == JsonValueKind.True)
            keywords.Add("keyboardFocusable");
        if (json.TryGetProperty("hasKeyboardFocus", out var hf) && hf.ValueKind == JsonValueKind.True)
            keywords.Add("focused");
        if (keywords.Count > 0)
            Console.Out.WriteLine($"state: {string.Join(" ", keywords)}");
    }

    private static void FormatSelector(JsonElement selector)
    {
        var stability = selector.TryGetProperty("stability", out var s) ? s.GetString() : null;
        var code = selector.TryGetProperty("code", out var c) ? c.GetString() : null;
        if (stability is null || code is null)
            return;

        Console.Out.WriteLine("selector:");
        Console.Out.WriteLine($"  stability: {stability}");
        Console.Out.WriteLine($"  code: {code}");
    }

    private static void FormatPatterns(JsonElement patterns)
    {
        var lines = new List<string>();

        foreach (var pattern in patterns.EnumerateObject())
        {
            var formatted = FormatSinglePattern(pattern.Name, pattern.Value);
            if (formatted is not null)
            {
                lines.Add($"  {pattern.Name}: {formatted}");
            }
        }

        if (lines.Count > 0)
        {
            Console.Out.WriteLine("patterns:");
            foreach (var line in lines)
            {
                Console.Out.WriteLine(line);
            }
        }
    }

    private static string? FormatSinglePattern(string name, JsonElement value)
    {
        return name switch
        {
            "Toggle" => FormatToggle(value),
            "ExpandCollapse" => FormatExpandCollapse(value),
            "SelectionItem" => FormatSelectionItem(value),
            "Selection" => FormatSelection(value),
            "Scroll" => FormatScroll(value),
            "GridItem" => FormatGridItem(value),
            "Table" => FormatTable(value),
            "Text" => FormatText(value),
            "Value" => FormatValue(value),
            "RangeValue" => FormatRangeValue(value),
            "Grid" => FormatGrid(value),
            _ => FormatGeneric(value),
        };
    }

    private static string? FormatToggle(JsonElement value)
    {
        if (value.TryGetProperty("ToggleState", out var state))
            return state.GetString() ?? state.GetRawText();
        return FormatGeneric(value);
    }

    private static string? FormatExpandCollapse(JsonElement value)
    {
        if (value.TryGetProperty("ExpandCollapseState", out var state))
            return state.GetString() ?? state.GetRawText();
        return FormatGeneric(value);
    }

    private static string? FormatSelectionItem(JsonElement value)
    {
        if (value.TryGetProperty("IsSelected", out var isSelected) && isSelected.ValueKind == JsonValueKind.True)
            return "Selected";
        return null;
    }

    private static string? FormatSelection(JsonElement value)
    {
        var parts = new List<string>();

        bool multiSelect = value.TryGetProperty("CanSelectMultiple", out var csm) && csm.ValueKind == JsonValueKind.True;
        bool selectionRequired = value.TryGetProperty("IsSelectionRequired", out var isr) && isr.ValueKind == JsonValueKind.True;

        if (multiSelect) parts.Add("MultiSelect");
        if (selectionRequired) parts.Add("SelectionRequired");

        if (value.TryGetProperty("SelectedItems", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            int count = items.GetArrayLength();
            if (count == 1 && !multiSelect)
            {
                parts.Add($"SelectedItem: {FormatScalarValue(items[0])}");
            }
            else if (count > 0)
            {
                var itemStrs = new List<string>();
                foreach (var item in items.EnumerateArray())
                    itemStrs.Add(FormatScalarValue(item));
                parts.Add($"SelectedItems: [{string.Join(", ", itemStrs)}]");
            }
        }

        return parts.Count > 0 ? $"{{{string.Join(", ", parts)}}}" : null;
    }

    private static string? FormatScroll(JsonElement value)
    {
        bool hCanScroll = value.TryGetProperty("HCanScroll", out var hcs) && hcs.ValueKind == JsonValueKind.True;
        bool vCanScroll = value.TryGetProperty("VCanScroll", out var vcs) && vcs.ValueKind == JsonValueKind.True;

        if (!hCanScroll && !vCanScroll) return null;

        var parts = new List<string>();
        if (hCanScroll)
        {
            var hPercent = value.TryGetProperty("HPercent", out var hp) ? hp.GetRawText() : "0";
            var hViewSize = value.TryGetProperty("HViewSize", out var hvs) ? hvs.GetRawText() : "0";
            parts.Add($"H: {{Percent: {hPercent}, ViewSize: {hViewSize}}}");
        }
        if (vCanScroll)
        {
            var vPercent = value.TryGetProperty("VPercent", out var vp) ? vp.GetRawText() : "0";
            var vViewSize = value.TryGetProperty("VViewSize", out var vvs) ? vvs.GetRawText() : "0";
            parts.Add($"V: {{Percent: {vPercent}, ViewSize: {vViewSize}}}");
        }

        return $"{{{string.Join(", ", parts)}}}";
    }

    private static string? FormatGridItem(JsonElement value)
    {
        var parts = new List<string>();

        if (value.TryGetProperty("Row", out var row))
            parts.Add($"Row: {row.GetRawText()}");
        if (value.TryGetProperty("Column", out var col))
            parts.Add($"Column: {col.GetRawText()}");
        if (value.TryGetProperty("RowSpan", out var rs) && rs.ValueKind == JsonValueKind.Number && rs.GetInt32() != 1)
            parts.Add($"RowSpan: {rs.GetRawText()}");
        if (value.TryGetProperty("ColumnSpan", out var cs) && cs.ValueKind == JsonValueKind.Number && cs.GetInt32() != 1)
            parts.Add($"ColumnSpan: {cs.GetRawText()}");

        return parts.Count > 0 ? $"{{{string.Join(", ", parts)}}}" : null;
    }

    private static string? FormatTable(JsonElement value)
    {
        var parts = new List<string>();

        if (value.TryGetProperty("RowOrColumnMajor", out var major) && major.ValueKind == JsonValueKind.String)
            parts.Add($"Major: \"{major.GetString()}\"");

        if (value.TryGetProperty("ColumnHeaders", out var colHeaders) && colHeaders.ValueKind == JsonValueKind.Array)
        {
            var headers = new List<string>();
            foreach (var h in colHeaders.EnumerateArray())
                headers.Add(FormatScalarValue(h));
            parts.Add($"ColumnHeaders: [{string.Join(", ", headers)}]");
        }

        if (value.TryGetProperty("RowHeaders", out var rowHeaders) && rowHeaders.ValueKind == JsonValueKind.Array && rowHeaders.GetArrayLength() > 0)
        {
            var headers = new List<string>();
            foreach (var h in rowHeaders.EnumerateArray())
                headers.Add(FormatScalarValue(h));
            parts.Add($"RowHeaders: [{string.Join(", ", headers)}]");
        }

        return parts.Count > 0 ? $"{{{string.Join(", ", parts)}}}" : null;
    }

    private static string? FormatText(JsonElement value)
    {
        var parts = new List<string>();

        if (value.TryGetProperty("Preview", out var preview))
            parts.Add($"Preview: {FormatScalarValue(preview)}");
        if (value.TryGetProperty("Length", out var length))
            parts.Add($"Length: {length.GetRawText()}");

        return parts.Count > 0 ? $"{{{string.Join(", ", parts)}}}" : null;
    }

    private static string? FormatValue(JsonElement value)
    {
        if (value.TryGetProperty("IsReadOnly", out var isReadOnly) && isReadOnly.ValueKind == JsonValueKind.True)
            return "{ReadOnly}";
        return null;
    }

    private static string? FormatRangeValue(JsonElement value)
    {
        var parts = new List<string>();

        if (value.TryGetProperty("Value", out var val))
            parts.Add($"Value: {val.GetRawText()}");
        if (value.TryGetProperty("Min", out var min))
            parts.Add($"Min: {min.GetRawText()}");
        if (value.TryGetProperty("Max", out var max))
            parts.Add($"Max: {max.GetRawText()}");
        if (value.TryGetProperty("SmallChange", out var sc))
            parts.Add($"SmallChange: {sc.GetRawText()}");
        if (value.TryGetProperty("LargeChange", out var lc))
            parts.Add($"LargeChange: {lc.GetRawText()}");
        if (value.TryGetProperty("IsReadOnly", out var isReadOnly) && isReadOnly.ValueKind == JsonValueKind.True)
            parts.Add("ReadOnly");

        return parts.Count > 0 ? $"{{{string.Join(", ", parts)}}}" : null;
    }

    private static string? FormatGrid(JsonElement value)
    {
        var parts = new List<string>();

        if (value.TryGetProperty("RowCount", out var rowCount))
            parts.Add($"RowCount: {rowCount.GetRawText()}");
        if (value.TryGetProperty("ColumnCount", out var colCount))
            parts.Add($"ColumnCount: {colCount.GetRawText()}");

        return parts.Count > 0 ? $"{{{string.Join(", ", parts)}}}" : null;
    }

    private static string? FormatGeneric(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var parts = new List<string>();
            foreach (var prop in value.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.True)
                    parts.Add(prop.Name);
                else if (prop.Value.ValueKind == JsonValueKind.False)
                    continue;
                else
                    parts.Add($"{prop.Name}: {FormatScalarValue(prop.Value)}");
            }
            return parts.Count > 0 ? $"{{{string.Join(", ", parts)}}}" : null;
        }

        return CliOutput.FormatJsonValue(value);
    }

    private static string FormatScalarValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => $"\"{value.GetString()}\"",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => value.GetRawText(),
        };
    }
}
