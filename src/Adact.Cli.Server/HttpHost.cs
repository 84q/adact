using System.Net;

using Adact.Engine;
using Adact.Mcp.Common;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

    /// <summary>
    /// 対話セッション判定で NG だった場合に返す終了コード (Adact.Cli/Output/ExitCodes.EnvironmentNotSupported と同値)。
    /// 設計: discussion/018_対話セッション判定.md §5.3。
    /// </summary>
    public const int ExitCodeEnvironmentNotSupported = 4;

    /// <summary>
    /// HTTP MCP サーバーをフォアグラウンドで起動し、<paramref name="ct"/> がキャンセルされるまで待機する。
    /// </summary>
    /// <param name="hostAddress">バインドする IP アドレス。</param>
    /// <param name="port">バインドする TCP ポート番号。</param>
    /// <param name="ct">サーバー停止を要求するキャンセルトークン。</param>
    /// <returns>
    /// プロセス終了コード。正常終了時は <c>0</c>。対話セッション判定 (018 §5.2) で NG だった場合は
    /// <see cref="ExitCodeEnvironmentNotSupported"/> (=4) を返し、HTTP listener は起動しない。
    /// </returns>
    /// <remarks>
    /// 起動前に <see cref="InteractiveSessionGuard.Probe"/> による対話デスクトップ判定を行う。
    /// NG の場合のエラーフォーマットは 009 §6.2 / 018 §5.3 に従い stderr に出力される。
    /// </remarks>
    public static async Task<int> RunAsync(IPAddress hostAddress, int port, CancellationToken ct)
    {
        // listener 起動前に対話デスクトップ判定を行う (018 §5.2)。
        if (!EnsureInteractiveSession())
        {
            return ExitCodeEnvironmentNotSupported;
        }

        var app = BuildApplication(hostAddress, port);
        await app.RunAsync(ct).ConfigureAwait(false);
        return 0;
    }

    /// <summary>
    /// 対話セッション判定を行い、NG なら stderr に既定のエラーフォーマット (009 §6.2 / 018 §5.3) で出力する。
    /// OK なら観測値を info ログとして stderr に1行記録する。
    /// </summary>
    /// <returns>対話デスクトップ判定が OK なら <see langword="true"/>、NG なら <see langword="false"/>。</returns>
    private static bool EnsureInteractiveSession()
    {
        var probe = InteractiveSessionGuard.Probe();
        if (!probe.Ok)
        {
            Console.Error.WriteLine($"error {InteractiveSessionGuard.ErrorCode}");
            Console.Error.WriteLine($"message {probe.Message}");
            Console.Error.WriteLine("hint launch 'adact serve' from the interactive logon session that owns the target GUI windows");
            return false;
        }

        Console.Error.WriteLine(
            $"info interactive session ok (SessionId={probe.SessionId}, WindowStation={probe.WindowStationName})");
        return true;
    }

    /// <summary>
    /// WebApplication を構築する。テストから WebApplicationFactory 経由で再利用できるよう Build/Run を分離。
    /// </summary>
    /// <param name="hostAddress">Kestrel が listen する IP アドレス。</param>
    /// <param name="port">Kestrel が listen する TCP ポート番号。</param>
    /// <returns>MCP エンドポイント (<see cref="McpPath"/>) がマップされた、未起動の <see cref="WebApplication"/>。</returns>
    /// <remarks>
    /// DI 登録: <see cref="UiaEngine"/>, <see cref="SessionStore"/>, <see cref="WindowRefStore"/>,
    /// <see cref="IDaemonControl"/> はいずれも Singleton。UIA 呼び出しは <see cref="UiaEngine"/> 内部の
    /// <c>SemaphoreSlim</c> で直列化される (Phase 4 サブタスク #2)。MCP サーバーは Streamable HTTP /
    /// Stateless モードで構築し、<see cref="WindowsTools"/> をツール実装として登録する。
    /// ログは全プロバイダを除外したうえで stderr 出力の SimpleConsole に寄せる。
    /// </remarks>
    public static WebApplication BuildApplication(IPAddress hostAddress, int port)
    {
        var builder = WebApplication.CreateBuilder();

        // 指定されたアドレスでバインド (既定は 127.0.0.1)。
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(hostAddress, port);
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

    /// <summary>
    /// 現在のアセンブリ (<see cref="HttpHost"/> を含む) のバージョンを文字列化して返す。MCP サーバーの
    /// <c>ServerInfo.Version</c> として通知される。バージョンが取得できない場合は <c>"0.0.0"</c> を返す。
    /// </summary>
    /// <returns>アセンブリバージョンの文字列表現。</returns>
    private static string ThisAssemblyVersion()
    {
        var v = typeof(HttpHost).Assembly.GetName().Version;
        return v?.ToString() ?? "0.0.0";
    }
}
