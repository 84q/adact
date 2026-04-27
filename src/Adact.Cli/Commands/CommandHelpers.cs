using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

internal static class CommandHelpers
{
    /// <summary>
    /// 接続解決 → MCP 接続 → コマンド実装の呼び出し、を共通化する。
    /// 解決失敗 → INVALID_ARGUMENT (exit 2)、接続失敗 → CONNECTION_FAILED (exit 3)、
    /// その他 → INTERNAL_ERROR (exit 1)。
    /// </summary>
    public static async Task<int> RunWithClientAsync(
        string? serverArg,
        Func<AdactMcpClient, CancellationToken, Task<int>> exec,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(exec);

        ServerEndpoint endpoint;
        try
        {
            endpoint = ConnectionResolver.Resolve(serverArg);
        }
        catch (InvalidUrlException ex)
        {
            return ConnectionErrors.ReportResolutionError(ex);
        }
        catch (ConfigParseException ex)
        {
            return ConnectionErrors.ReportResolutionError(ex);
        }

        try
        {
            await using var client = await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false);
            return await exec(client, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ConnectionErrors.ReportAndReturnExitCode(ex, endpoint);
        }
    }

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
