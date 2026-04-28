namespace Adact.Cli.Output;

/// <summary>
/// CLI エラー出力。設計 §6.2 の stderr key-value フォーマット (error / message / hint) で書き出す。
/// </summary>
internal sealed record CliError(string Code, string Message, string? Hint)
{
  public static void Write(string code, string message, string? hint = null)
  {
    Console.Error.WriteLine($"error {code}");
    Console.Error.WriteLine($"message {message}");
    if (!string.IsNullOrEmpty(hint))
    {
      Console.Error.WriteLine($"hint {hint}");
    }
  }
}
