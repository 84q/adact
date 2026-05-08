using System.Net.Sockets;

using Adact.Cli.Output;

namespace Adact.Cli.Connection;

/// <summary>
/// </summary>
internal static class ConnectionErrors
{
    /// <summary>
    /// </summary>
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
    /// </summary>
    public static int ReportResolutionError(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        CliError.Write(ErrorCodes.InvalidArgument, ex.Message);
        return ExitCodes.UserError;
    }

    private static bool IsConnectionFailure(Exception ex)
    {
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
