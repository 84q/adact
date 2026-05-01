namespace Adact.Cli.Output;

/// <summary>
/// CLI エラー出力。設計 §6.2 の stderr key-value フォーマット (error / message / hint) で書き出す。
/// </summary>
/// <param name="Code">エラーコード (例: <c>INVALID_ARGUMENT</c>)。</param>
/// <param name="Message">人間向けの説明文。</param>
/// <param name="Hint">対処方法のヒント。null/空のときは出力しない。</param>
internal sealed record CliError(string Code, string Message, string? Hint)
{
    /// <summary>
    /// エラー情報を stderr に "error/message/hint" の 1 行ずつの key-value 形式で書き出す。
    /// </summary>
    /// <param name="code">エラーコード文字列 (<see cref="ErrorCodes"/> の値を想定)。</param>
    /// <param name="message">人間向けメッセージ。</param>
    /// <param name="hint">対処方法のヒント。null/空なら hint 行は出力しない。</param>
    /// <remarks>
    /// stdout を汚さないように常に <see cref="Console.Error"/> に出力する。複数回呼んだ場合は
    /// その都度 3 行 (もしくは 2 行) が追記される。
    /// </remarks>
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
