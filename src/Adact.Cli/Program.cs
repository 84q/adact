using Adact.Cli.Server;
using Adact.Mcp.Stdio;

using Microsoft.Extensions.DependencyInjection;
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
            "serve" => await RunServeAsync(rest).ConfigureAwait(false),
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

    private static async Task<int> RunServeAsync(string[] args)
    {
        const int defaultPort = 41300;
        if (!TryParsePort(args, defaultPort, out var port, out var error))
        {
            Console.Error.WriteLine($"adact serve: {error}");
            return 1;
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        return await HttpHost.RunAsync(port, cts.Token).ConfigureAwait(false);
    }

    private static bool TryParsePort(string[] args, int defaultPort, out int port, out string? error)
    {
        port = defaultPort;
        error = null;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string? value = null;
            if (a == "--port")
            {
                if (i + 1 >= args.Length)
                {
                    error = "--port requires a value.";
                    return false;
                }
                value = args[++i];
            }
            else if (a.StartsWith("--port=", StringComparison.Ordinal))
            {
                value = a["--port=".Length..];
            }
            else
            {
                error = $"unknown option '{a}'.";
                return false;
            }

            if (!int.TryParse(value, out var parsed) || parsed < 0 || parsed > 65535)
            {
                error = $"invalid --port value '{value}' (expected 0-65535).";
                return false;
            }
            port = parsed;
        }
        return true;
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
        Console.Error.WriteLine("  serve [--port N]   Run as an HTTP MCP server on 127.0.0.1:N (default 41300).");
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
