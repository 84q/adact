using Adact.Engine;
using Adact.Mcp.Common;

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
  /// 対話セッション判定で NG だった場合に返す終了コード (Adact.Cli/Output/ExitCodes.EnvironmentNotSupported と同値)。
  /// 設計: discussion/018_対話セッション判定.md §5.3。
  /// </summary>
  public const int ExitCodeEnvironmentNotSupported = 4;

  /// <summary>
  /// stdio MCP サーバーを起動し、CancellationToken で終了するまで実行する。
  /// </summary>
  /// <param name="loggerFactory">CLI 側で stderr 出力に構成済の LoggerFactory。null なら DI 既定 (stderr 出力で再構成)。</param>
  /// <returns>正常終了は 0、対話デスクトップ非所属の場合は <see cref="ExitCodeEnvironmentNotSupported"/>。</returns>
  public static async Task<int> RunAsync(ILoggerFactory? loggerFactory, CancellationToken ct)
  {
    // listener 起動前に対話デスクトップ判定を行う (018 §5.2)。
    // stdio モードでは stdout を MCP プロトコルに専用するため、ログ・エラーは必ず stderr へ。
    if (!EnsureInteractiveSession())
    {
      return ExitCodeEnvironmentNotSupported;
    }

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
    builder.Services.AddSingleton<WindowRefStore>();
    builder.Services.AddSingleton<IDaemonControl, StdioDaemonControl>();

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
    return 0;
  }

  /// <summary>
  /// 対話セッション判定を行い、NG なら stderr に既定のエラーフォーマット (009 §6.2 / 018 §5.3) で出力して false を返す。
  /// OK なら観測値を info ログとして stderr に1行記録する。
  /// </summary>
  private static bool EnsureInteractiveSession()
  {
    var probe = InteractiveSessionGuard.Probe();
    if (!probe.Ok)
    {
      Console.Error.WriteLine($"error {InteractiveSessionGuard.ErrorCode}");
      Console.Error.WriteLine($"message {probe.Message}");
      Console.Error.WriteLine("hint launch 'adact local' from the interactive logon session that owns the target GUI windows");
      return false;
    }

    Console.Error.WriteLine(
        $"info interactive session ok (SessionId={probe.SessionId}, WindowStation={probe.WindowStationName})");
    return true;
  }

  private static string ThisAssemblyVersion()
  {
    var v = typeof(McpStdioServer).Assembly.GetName().Version;
    return v?.ToString() ?? "0.0.0";
  }
}
