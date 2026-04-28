using System.Net;

using Adact.Engine;
using Adact.Mcp.Common;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Adact.Cli.Server;

/// <summary>
/// HTTP MCP サーバー (Streamable HTTP / Stateless) のホスト構築・起動。
/// 設計: discussion/006_Phase4_設計.md §3-§5、Phase 5 は /mcp エンドポイント (009 §2.2)。
/// 127.0.0.1 にのみバインドし、ログは stderr に統一する。
/// </summary>
public static class HttpHost
{
  /// <summary>HTTP MCP のエンドポイントパス (009 §2.2)。</summary>
  public const string McpPath = "/mcp";

  public static async Task<int> RunAsync(int port, CancellationToken ct)
  {
    var app = BuildApplication(port);
    await app.RunAsync(ct).ConfigureAwait(false);
    return 0;
  }

  /// <summary>
  /// WebApplication を構築する。テストから WebApplicationFactory 経由で再利用できるよう Build/Run を分離。
  /// </summary>
  public static WebApplication BuildApplication(int port)
  {
    var builder = WebApplication.CreateBuilder();

    // localhost 既定でバインド: 127.0.0.1:<port> のみリッスン (009 §2)。
    builder.WebHost.ConfigureKestrel(options =>
    {
      options.Listen(IPAddress.Loopback, port);
    });

    // ログは stderr に統一 (stdio との一貫性のため)。stdout は HTTP モードでは
    // 占有していないが、運用ログを stdout に混ぜたくないので stderr に寄せる。
    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(o =>
    {
      o.SingleLine = true;
      o.IncludeScopes = false;
    });
    builder.Services.Configure<ConsoleLoggerOptions>(o =>
        o.LogToStandardErrorThreshold = LogLevel.Trace);

    // 共通 DI: Engine と SessionStore は Singleton。UiaEngine 内部の SemaphoreSlim
    // で UIA 呼び出しを直列化する (Phase 4 サブタスク #2 で導入済み)。
    builder.Services.AddSingleton<UiaEngine>(sp =>
        new UiaEngine(sp.GetRequiredService<ILoggerFactory>()));
    builder.Services.AddSingleton<SessionStore>();
    builder.Services.AddSingleton<WindowRefStore>();
    builder.Services.AddSingleton<IDaemonControl, HttpDaemonControl>();

    // MCP サーバー (Streamable HTTP / Stateless) を登録。
    builder.Services
        .AddMcpServer(o =>
        {
          o.ServerInfo = new ModelContextProtocol.Protocol.Implementation
          {
            Name = "adact",
            Version = ThisAssemblyVersion(),
          };
        })
        .WithHttpTransport(o => o.Stateless = true)
        .WithTools<WindowsTools>();

    var app = builder.Build();
    app.MapMcp(McpPath); // Phase 5: /mcp にマップ (009 §2.2)
    return app;
  }

  private static string ThisAssemblyVersion()
  {
    var v = typeof(HttpHost).Assembly.GetName().Version;
    return v?.ToString() ?? "0.0.0";
  }
}
