using Adact.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Adact.Mcp.Stdio;

/// <summary>
/// stdio MCP サーバーのエントリポイント。Cli から <see cref="RunAsync"/> を呼ぶ。
/// stdout は MCP JSON-RPC 専用、ログは stderr 経由 (呼び出し側で <see cref="ILoggerFactory"/> を構成済み)。
/// </summary>
public static class McpStdioServer
{
  /// <summary>
  /// stdio MCP サーバーを起動し、CancellationToken で終了するまで実行する。
  /// </summary>
  /// <param name="loggerFactory">CLI 側で stderr 出力に構成済の LoggerFactory。null なら DI 既定 (stderr 出力で再構成)。</param>
  public static async Task RunAsync(ILoggerFactory? loggerFactory, CancellationToken ct)
  {
    var settings = new HostApplicationBuilderSettings
    {
      DisableDefaults = true,
    };
    var builder = Host.CreateEmptyApplicationBuilder(settings);

    if (loggerFactory is not null)
    {
      builder.Services.AddSingleton(loggerFactory);
      builder.Services.AddSingleton(typeof(ILogger<>), typeof(Microsoft.Extensions.Logging.Logger<>));
    }
    else
    {
      builder.Logging.AddConsole(o =>
      {
        o.LogToStandardErrorThreshold = LogLevel.Trace;
      });
    }

    builder.Services.AddSingleton<UiaEngine>(sp =>
        new UiaEngine(sp.GetRequiredService<ILoggerFactory>()));
    builder.Services.AddSingleton<SessionStore>();

    builder.Services
        .AddMcpServer(o =>
        {
          o.ServerInfo = new ModelContextProtocol.Protocol.Implementation
          {
            Name = "adact",
            Version = ThisAssemblyVersion(),
          };
        })
        .WithStdioServerTransport()
        .WithTools<WindowsTools>();

    using var host = builder.Build();
    await host.RunAsync(ct).ConfigureAwait(false);
  }

  private static string ThisAssemblyVersion()
  {
    var v = typeof(McpStdioServer).Assembly.GetName().Version;
    return v?.ToString() ?? "0.0.0";
  }
}
