namespace Adact.Cli.Output;

/// <summary>
/// 設計 §5.3 の TSV (タブ区切り、ヘッダ行付き) を stdout に書き出す。
/// 空セルは "-" でプレースホルダ表示する。
/// </summary>
internal static class TsvWriter
{
    /// <summary>
    /// ヘッダ行 (列名をタブ区切り) を 1 行 stdout に書き出す。
    /// </summary>
    /// <param name="columns">左から並べる列名。</param>
    public static void WriteHeader(params string[] columns)
        => Console.Out.WriteLine(string.Join('\t', columns));

    /// <summary>
    /// データ行を 1 行 stdout に書き出す。
    /// </summary>
    /// <param name="cells">左から並べるセル値。null/空文字列は "-" に置換する。</param>
    public static void WriteRow(params string?[] cells)
        => Console.Out.WriteLine(
            string.Join('\t', cells.Select(c => string.IsNullOrEmpty(c) ? "-" : c)));
}
