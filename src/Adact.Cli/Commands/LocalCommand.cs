using System.CommandLine;

using Adact.Mcp.Stdio;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>local</c> サブコマンド。stdio を transport として MCP server を起動する (stdin/stdout = JSON-RPC)。
/// </summary>
internal static class LocalCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>local サブコマンド。</returns>
    public static Command Build()
    {
        var verbose = new Option<bool>("--verbose")
        {
            Description = "Enable Debug-level logging on stderr.",
        };

        var cmd = new Command("local", "Run as a stdio MCP server (stdin/stdout = JSON-RPC, stderr = logs). (--server option is ignored for this command.)");
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
