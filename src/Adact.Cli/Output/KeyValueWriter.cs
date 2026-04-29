namespace Adact.Cli.Output;

/// <summary>
/// 設計 §5.2 の "key value" 1 行形式を stdout に書き出す。
/// </summary>
internal static class KeyValueWriter
{
    /// <summary>
    /// <c>key value\n</c> の形で 1 行を <see cref="Console.Out"/> に書き出す。
    /// </summary>
    /// <param name="key">キー名 (空白を含めない想定)。</param>
    /// <param name="value">値。空白を含む場合もそのまま 1 行に書き出す。</param>
    public static void Write(string key, string value)
        => Console.Out.WriteLine($"{key} {value}");
}
