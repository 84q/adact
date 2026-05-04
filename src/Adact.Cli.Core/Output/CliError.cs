namespace Adact.Cli.Output;

/// <summary>
/// CLI エラー出力。stdout に統一された yaml風エラー形式で書き出す。
/// </summary>
/// <param name="Code">エラーコード (例: <c>INVALID_ARGUMENT</c>)。</param>
/// <param name="Message">人間向けの説明文。</param>
/// <param name="Hint">対処方法のヒント。null/空のときは出力しない。</param>
internal sealed record CliError(string Code, string Message, string? Hint)
{
    /// <summary>
    /// エラー情報を stdout に yaml風で書き出す。
    /// </summary>
    /// <param name="code">エラーコード文字列 (<see cref="ErrorCodes"/> の値を想定)。</param>
    /// <param name="message">人間向けメッセージ。</param>
    /// <param name="hint">対処方法のヒント。null/空なら hint 行は出力しない。</param>
    public static void Write(string code, string message, string? hint = null)
        => CliOutput.WriteYamlFailure(code, message, hint);
}
