namespace Adact.Cli.Output;

/// <summary>
/// </summary>
internal sealed record CliError(string Code, string Message, string? Hint)
{
    /// <summary>
    /// </summary>
    public static void Write(string code, string message, string? hint = null)
        => CliOutput.WriteYamlFailure(code, message, hint);
}
