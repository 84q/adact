using System.Globalization;
using System.Text.Json;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class JsonHelpers
{
    /// <summary>
    /// </summary>
    public static string? GetStringOrNull(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        if (!obj.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => v.ToString(),
        };
    }

    /// <summary>
    /// </summary>
    public static string? GetIntAsStringOrNull(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        if (!obj.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.TryGetInt64(out var l)
                ? l.ToString(CultureInfo.InvariantCulture)
                : v.GetRawText(),
            JsonValueKind.String => v.GetString(),
            _ => null,
        };
    }

    public static int? GetIntOrNull(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        if (!obj.TryGetProperty(name, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
        return null;
    }
}
