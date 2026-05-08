namespace Adact.Cli.Output;

/// <summary>
/// </summary>
internal static class TsvWriter
{
    /// <summary>
    /// </summary>
    public static void WriteHeader(params string[] columns)
        => Console.Out.WriteLine(string.Join('\t', columns));

    /// <summary>
    /// </summary>
    public static void WriteRow(params string?[] cells)
        => Console.Out.WriteLine(
            string.Join('\t', cells.Select(c => string.IsNullOrEmpty(c) ? "-" : c)));
}
