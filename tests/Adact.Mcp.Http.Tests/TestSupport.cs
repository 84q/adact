using System.Text.Json;

namespace Adact.Mcp.Http.Tests;

internal static class SampleAppWindowFinder
{
    /// <summary>Performs the Find Window Ref operation.</summary>
    public static string? FindWindowRef(string listText)
    {
        using var listDoc = JsonDocument.Parse(listText);
        foreach (var item in listDoc.RootElement.EnumerateArray())
        {
            var processName = TryGetString(item, "processName");
            var windowTitle = TryGetString(item, "windowTitle");
            if (!IsSampleAppWindow(processName, windowTitle))
            {
                continue;
            }

            var windowRef = TryGetString(item, "windowRef");
            if (!string.IsNullOrWhiteSpace(windowRef))
            {
                return windowRef;
            }
        }

        return null;
    }

    private static string? TryGetString(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool IsSampleAppWindow(string? processName, string? windowTitle)
    {
        return (processName?.Contains("SampleApp", StringComparison.OrdinalIgnoreCase) ?? false)
            || (windowTitle?.Contains("ADACT SampleApp", StringComparison.Ordinal) ?? false);
    }
}
