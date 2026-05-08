using System.Text.RegularExpressions;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class RefValidator
{
    private static readonly Regex ElementRefRegex = new(@"^s\d+e\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SessionRefRegex = new(@"^s\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex WindowRefRegex = new(@"^w\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SidExtractRegex = new(@"^s(\d+)e\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsElementRef(string? s) => s is not null && ElementRefRegex.IsMatch(s);

    public static bool IsSessionRef(string? s) => s is not null && SessionRefRegex.IsMatch(s);

    public static bool IsWindowRef(string? s) => s is not null && WindowRefRegex.IsMatch(s);

    public static string? ExtractSessionId(string? elementRef)
    {
        if (elementRef is null) return null;
        var m = SidExtractRegex.Match(elementRef);
        return m.Success ? "s" + m.Groups[1].Value : null;
    }
}
