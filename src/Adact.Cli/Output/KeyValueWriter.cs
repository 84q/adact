namespace Adact.Cli.Output;

/// <summary>
/// 設計 §5.2 の "key value" 1 行形式を stdout に書き出す。
/// </summary>
internal static class KeyValueWriter
{
  public static void Write(string key, string value)
      => Console.Out.WriteLine($"{key} {value}");
}
