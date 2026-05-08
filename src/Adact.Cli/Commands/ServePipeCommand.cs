using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;
using Adact.Cli.Server.NamedPipe;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class ServePipeCommand
{
    private const int PipeExistenceCheckTimeoutMs = 100;

    /// <summary>
    /// </summary>
    internal static Func<NamedPipeEndPoint, int, CancellationToken, Task<bool>> IsServerRunningAsync { get; set; }
        = NamedPipeMcpClient.IsServerRunningAsync;

    /// <summary>
    /// </summary>
    internal static Func<string, CancellationToken, Task<int>> RunNamedPipeHostAsync { get; set; }
        = NamedPipeHost.RunAsync;

    public static Command Build()
    {
        var cmd = new Command("pipe", "Run as a Named Pipe MCP server. Pipe name is auto-generated from workspace hash. (--server option is ignored for this command.)");

        cmd.SetAction(async (parseResult, ct) =>
        {
            var workspacePath = NamedPipeEndPoint.ResolveWorkspacePath();
            var endpoint = NamedPipeEndPoint.FromWorkspacePath(workspacePath);

            if (await IsServerRunningAsync(endpoint, PipeExistenceCheckTimeoutMs, ct).ConfigureAwait(false))
            {
                CliError.Write(
                    ErrorCodes.AlreadyRunning,
                    "A daemon is already running for this workspace.",
                    "Use 'adact daemon-stop' to stop the existing daemon first.");
                return ExitCodes.CommandFailed;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                return await RunNamedPipeHostAsync(endpoint.PipeName, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CliError.Write(ErrorCodes.InternalError, ex.Message);
                return ExitCodes.CommandFailed;
            }
        });

        return cmd;
    }
}
