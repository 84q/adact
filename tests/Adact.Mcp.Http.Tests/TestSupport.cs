using System.Text.Json;

namespace Adact.Mcp.Http.Tests;

internal static class CalculatorWindowFinder
{
    public static string? FindWindowRef(string listText)
    {
        using var listDoc = JsonDocument.Parse(listText);
        foreach (var item in listDoc.RootElement.EnumerateArray())
        {
            var processName = TryGetString(item, "processName");
            var windowTitle = TryGetString(item, "windowTitle");
            if (!IsCalculatorWindow(processName, windowTitle))
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

    private static bool IsCalculatorWindow(string? processName, string? windowTitle)
    {
        return ContainsCalculatorToken(processName)
            || ContainsCalculatorToken(windowTitle)
            || (windowTitle?.Contains("電卓", StringComparison.Ordinal) ?? false);
    }

    private static bool ContainsCalculatorToken(string? value)
    {
        return value?.Contains("Calculator", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
