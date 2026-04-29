using System.CommandLine;

using Adact.Mcp.Stdio;

namespace Adact.Cli.Commands;

internal static class LocalCommand
{
    public static Command Build()
    {
        var verbose = new Option<bool>("--verbose")
        {
            Description = "Enable Debug-level logging on stderr.",
        };

        var cmd = new Command("local", "Run as a stdio MCP server (stdin/stdout = JSON-RPC, stderr = logs).");
        cmd.Options.Add(verbose);

        cmd.SetAction(async (parseResult, ct) =>
        {
            var v = parseResult.GetValue(verbose);
            using var loggerFactory = LoggerFactoryHelper.Create(v);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
            return await McpStdioServer.RunAsync(loggerFactory, cts.Token).ConfigureAwait(false);
        });

        return cmd;
    }
}
