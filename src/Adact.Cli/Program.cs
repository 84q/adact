using Adact.Mcp.Stdio;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Adact.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // 簡易引数解析: 第 1 引数をサブコマンドとして扱い、残りを各サブコマンドに渡す。
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var subcommand = args[0];
        var rest = args[1..];

        return subcommand switch
        {
            "local" => await RunLocalAsync(rest).ConfigureAwait(false),
            "serve" => RunServePlaceholder(rest),
            _ => UnknownSubcommand(subcommand),
        };
    }

    private static async Task<int> RunLocalAsync(string[] args)
    {
        var verbose = args.Contains("--verbose");
        using var loggerFactory = CreateLoggerFactory(verbose);
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        await McpStdioServer.RunAsync(loggerFactory, cts.Token).ConfigureAwait(false);
        return 0;
    }

    private static int RunServePlaceholder(string[] args)
    {
        // Phase 4 サブタスク 4 で本実装予定。現時点では placeholder として exit 1 を返す。
        Console.Error.WriteLine("adact serve: not implemented yet (Phase 4 サブタスク 4 で実装予定).");
        return 1;
    }

    private static int UnknownSubcommand(string subcommand)
    {
        Console.Error.WriteLine($"adact: unknown subcommand '{subcommand}'.");
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: adact <subcommand> [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Subcommands:");
        Console.Error.WriteLine("  local              Run as a stdio MCP server (stdin/stdout = JSON-RPC, stderr = logs).");
        Console.Error.WriteLine("  serve [--port N]   Run as an HTTP MCP server (not implemented yet).");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Common options:");
        Console.Error.WriteLine("  --verbose          Enable Debug-level logging on stderr.");
    }

    private static ILoggerFactory CreateLoggerFactory(bool verbose)
    {
        return LoggerFactory.Create(b =>
        {
            b.AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.IncludeScopes = false;
            });
            // 全レベルを stderr に流す (stdout はデータ出力用)
            b.Services.Configure<ConsoleLoggerOptions>(o =>
                o.LogToStandardErrorThreshold = LogLevel.Trace);
            b.AddFilter((category, level) =>
            {
                var threshold = verbose ? LogLevel.Debug : LogLevel.Warning;
                return level >= threshold;
            });
        });
    }
}
