using System.Text.RegularExpressions;

namespace Adact.Engine;

/// <summary>
/// <see cref="UiaEngine.WaitForWindowAsync"/> 用の window 検索条件。
/// 各フィールドは正規表現で部分一致判定される (null/空は無視)。
/// 少なくとも 1 つのフィールドを設定する必要がある。
/// </summary>
/// <param name="Title">window title 正規表現。</param>
/// <param name="ClassName">Win32 ClassName 正規表現。</param>
/// <param name="ProcessName">プロセス名 (拡張子なし) 正規表現。</param>
/// <param name="Executable">プロセスのフルパスに対する正規表現。</param>
public sealed record WindowSearchQuery(
    string? Title = null,
    string? ClassName = null,
    string? ProcessName = null,
    string? Executable = null)
{
    /// <summary>少なくとも 1 つのフィールドが設定されているなら true。</summary>
    public bool HasAnyCondition =>
        !string.IsNullOrEmpty(Title)
        || !string.IsNullOrEmpty(ClassName)
        || !string.IsNullOrEmpty(ProcessName)
        || !string.IsNullOrEmpty(Executable);

    /// <summary>
    /// <see cref="WindowInfo"/> が本検索条件のすべての非 null フィールドにマッチするか判定する。
    /// </summary>
    /// <param name="info">判定対象。</param>
    /// <param name="executablePath">対象プロセスのフルパス (取得失敗時は null)。</param>
    /// <returns>すべての設定済みフィールドが match すれば true。</returns>
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
