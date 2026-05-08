using System.Text.Json;

using ModelContextProtocol.Protocol;

namespace Adact.Cli.Output;

/// <summary>
/// </summary>
internal static class McpResponse
{
    /// <summary>
    /// </summary>
    public static JsonElement GetJson(CallToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.StructuredContent is JsonElement je && je.ValueKind != JsonValueKind.Undefined)
        {
            return je;
        }

        if (result.Content is { Count: > 0 } content
            && content[0] is TextContentBlock tcb
            && !string.IsNullOrEmpty(tcb.Text))
        {
            return JsonSerializer.Deserialize<JsonElement>(tcb.Text);
        }

        throw new InvalidOperationException("MCP response has neither structured content nor text content.");
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    /// </remarks>
    public static int? TryReportError(CallToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsError != true)
        {
            return null;
        }

        var code = ErrorCodes.InternalError;
        var message = "tool reported error";
        string? hint = null;

        try
        {
            var json = GetJson(result);
            if (json.ValueKind == JsonValueKind.Object)
            {
                if (json.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String)
                {
                    code = c.GetString() ?? code;
                }
                if (json.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                {
                    message = m.GetString() ?? message;
                }
                if (json.TryGetProperty("hint", out var h) && h.ValueKind == JsonValueKind.String)
                {
                    hint = h.GetString();
                }
            }
        }
        catch (JsonException)
        {
            if (result.Content is { Count: > 0 } content
                && content[0] is TextContentBlock tcb
                && !string.IsNullOrEmpty(tcb.Text))
            {
                message = tcb.Text;
            }
        }
        catch (InvalidOperationException)
        {
            if (result.Content is { Count: > 0 } content
                && content[0] is TextContentBlock tcb
                && !string.IsNullOrEmpty(tcb.Text))
            {
                message = tcb.Text;
            }
        }

        CliError.Write(code, message, hint);
        return ExitCodes.CommandFailed;
    }
}
