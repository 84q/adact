using System.CommandLine;
using System.Text.Json;
using Adact.Engine;
using Adact.Mcp.Stdio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Adact.Cli;

internal static class Program
{
  public static async Task<int> Main(string[] args)
  {
    var verboseOption = new Option<bool>(
        name: "--verbose",
        description: "Enable verbose (Debug) logging on stderr.");

    var jsonOption = new Option<bool>(
        name: "--json",
        description: "Emit machine-readable JSON.");

    var processOption = new Option<string>(
        name: "--process",
        description: "Target process name (e.g. notepad++, CalculatorApp).")
    { IsRequired = true };

    var filterOption = new Option<string>(
        name: "--filter",
        description: "Snapshot filter strategy: operable (default) or raw.",
        getDefaultValue: () => "operable");

    var minifiedOption = new Option<bool>(
        name: "--minified",
        description: "Emit a single-line JSON instead of pretty-printed.");

    var listCmd = new Command("list", "List top-level windows.")
        {
            jsonOption,
        };
    listCmd.SetHandler(async (bool json, bool verbose) =>
    {
      using var loggerFactory = CreateLoggerFactory(verbose);
      using var engine = new UiaEngine(loggerFactory);
      var windows = await engine.ListWindowsAsync().ConfigureAwait(false);

      if (json)
      {
        var dto = windows.Select(w => new
        {
          pid = w.ProcessId,
          processName = w.ProcessName,
          title = w.Title,
          controlType = w.ControlType,
          className = w.ClassName,
          hwnd = w.NativeWindowHandle.ToInt64(),
        });
        Console.WriteLine(JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true }));
      }
      else
      {
        Console.WriteLine($"Top-level windows: {windows.Count}");
        foreach (var w in windows)
        {
          Console.WriteLine($"  pid={w.ProcessId,-6} proc={w.ProcessName,-24} ctrl={w.ControlType,-12} title=\"{w.Title}\"");
        }
      }
    }, jsonOption, verboseOption);

    var snapshotCmd = new Command("snapshot", "Snapshot a single window.")
        {
            processOption,
            filterOption,
            minifiedOption,
        };
    snapshotCmd.SetHandler(async (string processName, string filter, bool minified, bool verbose) =>
    {
      using var loggerFactory = CreateLoggerFactory(verbose);
      using var engine = new UiaEngine(loggerFactory);
      using var session = await engine.AttachAsync(AttachQuery.ByProcess(processName)).ConfigureAwait(false);
      var result = await session.SnapshotAsync(new SnapshotOptions(FilterName: filter)).ConfigureAwait(false);

      if (minified)
      {
        Console.WriteLine(result.Json);
      }
      else
      {
        using var doc = JsonDocument.Parse(result.Json);
        Console.WriteLine(JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true }));
      }
    }, processOption, filterOption, minifiedOption, verboseOption);

    var rootCmd = new RootCommand("ADACT — Windows desktop UI automation CLI (Phase 2 prototype).")
        {
            listCmd,
            snapshotCmd,
        };
    rootCmd.AddGlobalOption(verboseOption);

    var localOption = new Option<bool>(
        name: "--local",
        description: "Run as a stdio MCP server. stdin/stdout speak JSON-RPC; logs go to stderr.");
    rootCmd.AddGlobalOption(localOption);

    // --local が指定されたら他サブコマンドより優先して MCP サーバーを起動する。
    rootCmd.SetHandler(async (bool local, bool verbose) =>
    {
      if (!local) return;
      using var loggerFactory = CreateLoggerFactory(verbose);
      using var cts = new CancellationTokenSource();
      Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
      await McpStdioServer.RunAsync(loggerFactory, cts.Token).ConfigureAwait(false);
    }, localOption, verboseOption);

    return await rootCmd.InvokeAsync(args).ConfigureAwait(false);
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
