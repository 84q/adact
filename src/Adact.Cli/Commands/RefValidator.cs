using System.Text.RegularExpressions;

namespace Adact.Cli.Commands;

/// <summary>
/// Ref ID 形式チェック共通化。設計 009 §5.4 / §6.3 / §7.8。
/// </summary>
internal static class RefValidator
{
    /// <summary>Element Ref 形式 (<c>s&lt;sid&gt;e&lt;eid&gt;</c>) マッチャー。</summary>
    private static readonly Regex ElementRefRegex = new(@"^s\d+e\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Session Ref 形式 (<c>s&lt;sid&gt;</c>) マッチャー。</summary>
    private static readonly Regex SessionRefRegex = new(@"^s\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Window Ref 形式 (<c>w&lt;n&gt;</c>) マッチャー。</summary>
    private static readonly Regex WindowRefRegex = new(@"^w\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Element Ref から sid 部分を取り出すためのキャプチャ付きマッチャー。</summary>
    private static readonly Regex SidExtractRegex = new(@"^s(\d+)e\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Element Ref 形式か判定する。null は常に false。</summary>
    /// <param name="s">検査対象文字列。</param>
    /// <returns>Element Ref 形式に一致すれば true。</returns>
    public static bool IsElementRef(string? s) => s is not null && ElementRefRegex.IsMatch(s);

    /// <summary>Session Ref 形式か判定する。null は常に false。</summary>
    /// <param name="s">検査対象文字列。</param>
    /// <returns>Session Ref 形式に一致すれば true。</returns>
    public static bool IsSessionRef(string? s) => s is not null && SessionRefRegex.IsMatch(s);

    /// <summary>Window Ref 形式か判定する。null は常に false。</summary>
    /// <param name="s">検査対象文字列。</param>
    /// <returns>Window Ref 形式に一致すれば true。</returns>
    public static bool IsWindowRef(string? s) => s is not null && WindowRefRegex.IsMatch(s);

    /// <summary>"s1e3" -&gt; "s1"。Element Ref でなければ null。</summary>
    /// <param name="elementRef">Element Ref 文字列。</param>
    /// <returns>対応する Session Ref、もしくは null。</returns>
    public static string? ExtractSessionId(string? elementRef)
    {
        if (elementRef is null) return null;
        var m = SidExtractRegex.Match(elementRef);
        return m.Success ? "s" + m.Groups[1].Value : null;
    }
}
