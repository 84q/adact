using System.CommandLine;
using System.IO.Pipes;

using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

namespace Adact.Cli.Commands;

/// <summary>
/// Builds and executes the <c>daemon-stop</c> command.
/// </summary>
internal static class DaemonStopCommand
{
    private const int PostStopDelayMs = 500;
    private const int PostStopServerCheckTimeoutMs = 1000;
    private const int PipeExistenceCheckTimeoutMs = 100;
    public static Command Build()
    {
        var cmd = new Command("daemon-stop", "Stop a local Named Pipe MCP daemon gracefully.");

        cmd.SetAction((parseResult, ct) =>
        {
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);
            return RunAsync(serverArg, ct);
        });

        return cmd;
    }

    /// <param name="ct">cancellation token。</param>
    /// <param name="serverArg">The optional server endpoint override.</param>
    /// <returns>exit code。</returns>
    private static async Task<int> RunAsync(string? serverArg, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(serverArg))
        {
            CliError.Write(
                ErrorCodes.LocalOnly,
                "daemon-stop is not supported for HTTP mode. Use Ctrl+C to stop the server.",
                "For HTTP server, stop the process manually or use task management tools.");
            return ExitCodes.UserError;
        }

        var endpoint = ConnectionResolver.ResolveNamedPipeEndpoint();

        await using var client = await ConnectNamedPipeAsync(endpoint, ct).ConfigureAwait(false);
        if (client is null)
        {
            CliOutput.WriteYamlSuccess(metaFields: null, [CliOutput.Field("stopped", "false"), CliOutput.Field("message", "No daemon is running")]);
            return ExitCodes.Success;
        }

        CallToolResult result;
        try
        {
            result = await client.CallToolAsync("adact_daemon_stop", arguments: null, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested && IsConnectionDropException(ex))
        {
            CliOutput.WriteYamlSuccess(metaFields: null, [CliOutput.Field("stopped", "true")]);
            return ExitCodes.Success;
        }

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        await Task.Delay(PostStopDelayMs, ct).ConfigureAwait(false);

        var isRunning = await NamedPipeMcpClient.IsServerRunningAsync(endpoint, PostStopServerCheckTimeoutMs, ct).ConfigureAwait(false);
        if (isRunning)
        {
            CliError.Write(
                ErrorCodes.InternalError,
                "Server did not stop after adact_daemon_stop command.",
                "The adact_daemon_stop command was sent but the server is still running.");
            return ExitCodes.CommandFailed;
        }

        CliOutput.WriteYamlSuccess(metaFields: null, [CliOutput.Field("stopped", "true")]);
        return ExitCodes.Success;
    }

    private static async Task<NamedPipeMcpClient?> ConnectNamedPipeAsync(NamedPipeEndPoint endpoint, CancellationToken ct)
    {
        var isRunning = await NamedPipeMcpClient.IsServerRunningAsync(endpoint, PipeExistenceCheckTimeoutMs, ct).ConfigureAwait(false);
        if (!isRunning)
        {
            return null; // no pipe means return immediately
        }

        try
        {
            return await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Determines whether an exception was caused by the daemon closing the connection.
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns><see langword="true"/> when the exception represents a dropped connection.</returns>
    internal static bool IsConnectionDropException(Exception ex)
    {
        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is IOException
                || cur is ObjectDisposedException)
            {
                return true;
            }
        }
        return false;
    }
}
