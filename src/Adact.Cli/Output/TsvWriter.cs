namespace Adact.Cli.Output;

/// <summary>
/// 設計 §5.3 の TSV (タブ区切り、ヘッダ行付き) を stdout に書き出す。
/// 空セルは "-" でプレースホルダ表示する。
/// </summary>
internal static class TsvWriter
{
    public static void WriteHeader(params string[] columns)
        => Console.Out.WriteLine(string.Join('\t', columns));

    public static void WriteRow(params string?[] cells)
        => Console.Out.WriteLine(
            string.Join('\t', cells.Select(c => string.IsNullOrEmpty(c) ? "-" : c)));
}
