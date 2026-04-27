using Adact.Cli.Output;

namespace Adact.Cli.Commands;

internal static class CommandHelpers
{
    /// <summary>
    /// Phase 5 #3 (CLI 骨格) のスタブ用ヘルパ。各コマンドの本実装は #4-#8 で順次差し替わるため、
    /// それまでの暫定として ErrorCodes.InternalError + "not implemented yet" メッセージを返す。
    /// </summary>
    public static int NotYetImplemented(string commandName)
    {
        CliError.Write(
            ErrorCodes.InternalError,
            $"{commandName}: not implemented yet (Phase 5 in progress).");
        return ExitCodes.CommandFailed;
    }
}
