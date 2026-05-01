using System.Net.Sockets;

using Adact.Cli.Output;

namespace Adact.Cli.Connection;

/// <summary>
/// 接続層で発生した例外を CLI のエラーコード / exit code にマッピングするヘルパ。
/// 設計 009 §6.x。
/// </summary>
internal static class ConnectionErrors
{
    /// <summary>
    /// daemon への接続失敗系例外を <c>CONNECTION_FAILED</c> (exit 3) として stderr に書き出す。
    /// それ以外の例外は <c>INTERNAL_ERROR</c> (exit 1)。
    /// </summary>
    /// <param name="ex">発生した例外。</param>
    /// <param name="endpoint">接続試行先の endpoint。メッセージ出力に使用する。</param>
    /// <returns>接続失敗なら <see cref="ExitCodes.ConnectionFailed"/>、それ以外は <see cref="ExitCodes.CommandFailed"/>。</returns>
    public static int ReportAndReturnExitCode(Exception ex, ServerEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(ex);
        ArgumentNullException.ThrowIfNull(endpoint);

        if (IsConnectionFailure(ex))
        {
            CliError.Write(
                ErrorCodes.ConnectionFailed,
                $"Failed to connect to {endpoint.Url}: {ex.Message}",
                "ensure 'adact serve' is running on the target host.");
            return ExitCodes.ConnectionFailed;
        }

        CliError.Write(
            ErrorCodes.InternalError,
            $"Unexpected error while connecting to {endpoint.Url}: {ex.Message}");
        return ExitCodes.CommandFailed;
    }

    /// <summary>
    /// <see cref="ConnectionResolver.Resolve"/> 周辺で発生する URL / config 検証エラーを
    /// <c>INVALID_ARGUMENT</c> (exit 2) として stderr に書き出す。
    /// </summary>
    /// <param name="ex">接続解決時に発生した例外。</param>
    /// <returns>常に <see cref="ExitCodes.UserError"/>。</returns>
    public static int ReportResolutionError(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        CliError.Write(ErrorCodes.InvalidArgument, ex.Message);
        return ExitCodes.UserError;
    }

    /// <summary>接続系例外かを判定する。.NET の HTTP / socket 系は inner にラップされて来ることが多いため chain を walk する。</summary>
    /// <param name="ex">判定対象の例外。</param>
    /// <returns>HTTP / socket / Cancellation 系と見なせる例外チェーンなら true。</returns>
    private static bool IsConnectionFailure(Exception ex)
    {
        // .NET の HTTP / socket 系は inner にラップされて来ることが多いので chain を walk する。
        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is HttpRequestException
                || cur is SocketException
                || cur is TaskCanceledException
                || cur is OperationCanceledException)
            {
                return true;
            }
        }
        return false;
    }
}
