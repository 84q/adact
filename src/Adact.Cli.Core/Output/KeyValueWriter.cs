namespace Adact.Cli.Output;

/// <summary>
/// </summary>
internal static class KeyValueWriter
{
    /// <summary>
    /// </summary>
    public static void Write(string key, string value)
        => Console.Out.WriteLine($"{key} {value}");
}
