using System.Text.RegularExpressions;

namespace Adact.Engine;

/// <summary>
/// Describes criteria used to find a top-level window.
/// </summary>
public sealed record WindowSearchQuery(
    string? Title = null,
    string? ClassName = null,
    string? ProcessName = null,
    string? Executable = null)
{
    /// <summary>
    /// Gets whether at least one condition is set.
    /// </summary>
    public bool HasAnyCondition =>
        !string.IsNullOrEmpty(Title)
        || !string.IsNullOrEmpty(ClassName)
        || !string.IsNullOrEmpty(ProcessName)
        || !string.IsNullOrEmpty(Executable);

    /// <summary>
    /// Checks whether the query matches a window.
    /// </summary>
    public bool Matches(WindowInfo info, string? executablePath)
    {
        if (!string.IsNullOrEmpty(Title) && !RegexMatch(Title!, info.Title)) return false;
        if (!string.IsNullOrEmpty(ClassName) && !RegexMatch(ClassName!, info.ClassName ?? string.Empty)) return false;
        if (!string.IsNullOrEmpty(ProcessName) && !RegexMatch(ProcessName!, info.ProcessName)) return false;
        if (!string.IsNullOrEmpty(Executable) && !RegexMatch(Executable!, executablePath ?? string.Empty)) return false;
        return true;
    }

    private static bool RegexMatch(string pattern, string input)
    {
        try { return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase); }
        catch (ArgumentException) { return false; }
    }
}
