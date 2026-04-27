using System.CommandLine;

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

    /// <summary>
    /// 共通 <c>--server</c> Option。設計 009 §3 / §4.2。
    /// 各コマンドはこのヘルパで Option を生成し、AddOption で root に登録する。
    /// </summary>
    public static Option<string?> CreateServerOption() =>
        new("--server")
        {
            Description = "Connection target URL (e.g. http://127.0.0.1:41300/mcp). "
                + "Falls back to .adact/config.json or the default endpoint.",
        };
}
