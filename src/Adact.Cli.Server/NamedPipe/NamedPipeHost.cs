using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

using Adact.Engine;
using Adact.Mcp.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Adact.Cli.Server.NamedPipe;

/// <summary>
/// Named Pipe による MCP サーバーのホスト構築・起動。
/// StreamServerTransport を使用して MCP サーバーを提供する。
/// 設計: discussion/033_NamedPipe_HTTP_統合設計.md §2
/// </summary>
public static class NamedPipeHost
{
    /// <summary>
    /// 対話セッション判定で NG だった場合に返す終了コード。
    /// </summary>
    public const int ExitCodeEnvironmentNotSupported = 4;

    private const int ConnectionRetryDelayMs = 100;

    /// <summary>
    /// Named Pipe MCP サーバーをフォアグラウンドで起動し、<paramref name="ct" /> がキャンセルされるまで待機する。
    /// </summary>
    /// <param name="pipeName">Named Pipe の名前（例: \\.\pipe\adact-{hash}-default）。</param>
    /// <param name="ct">サーバー停止を要求するキャンセルトークン。</param>
    /// <returns>
    /// プロセス終了コード。正常終了時は <c>0</c>。対話セッション判定で NG だった場合は
    /// <see cref="ExitCodeEnvironmentNotSupported" /> (=4) を返し、サーバーは起動しない。
    /// </returns>
    /// <remarks>
    /// 起動前に <see cref="InteractiveSessionGuard.Probe" /> による対話デスクトップ判定を行う。
    /// NG の場合のエラーフォーマットは stderr に出力される。
    /// </remarks>
    public static async Task<int> RunAsync(string pipeName, CancellationToken ct)
    {
        // listener 起動前に対話デスクトップ判定を行う
        if (!EnsureInteractiveSession())
        {
            return ExitCodeEnvironmentNotSupported;
        }

        var loggerFactory = CreateLoggerFactory();
        var logger = loggerFactory.CreateLogger(typeof(NamedPipeHost));

        logger.LogInformation("Starting Named Pipe MCP server on {PipeName}", pipeName);

        try
        {
            await RunServerAsync(pipeName, loggerFactory, ct).ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Server stopped (cancelled)");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Server error");
            Console.Error.WriteLine($"error INTERNAL_ERROR");
            Console.Error.WriteLine($"message {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// サーバーのメインループを実行する。
    /// </summary>
    private static async Task RunServerAsync(string pipeName, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        // パイプ名から "\\.\pipe\" プレフィックスを除去
        var shortPipeName = pipeName;
        if (shortPipeName.StartsWith(@"\\.\pipe\", StringComparison.Ordinal))
        {
            shortPipeName = shortPipeName[@"\\.\pipe\".Length..];
        }

        // 親サービスプロバイダーを構築
        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton<UiaEngine>(sp => new UiaEngine(sp.GetRequiredService<ILoggerFactory>()));
        services.AddSingleton<SessionStore>();
        services.AddSingleton<WindowRefStore>();
        using var parentServiceProvider = services.BuildServiceProvider();

        // ConcurrentDictionary を使用して thread-safe に接続を管理
        var connections = new ConcurrentDictionary<Guid, NamedPipeConnection>();

        // adact_daemon_stop からキャンセルできるように、外部トークンとリンクした CTS を作成
        using var serverCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var serverCt = serverCts.Token;

        try
        {
            // 起動完了メッセージを出力（クライアントの自動起動検知用）
            Console.WriteLine($"### Success");
            Console.WriteLine($"Daemon listening on {pipeName}");

            while (!serverCt.IsCancellationRequested)
            {
                // 新しい接続を待ち受け
                var pipeStream = NamedPipeServerStreamAcl.Create(
                    shortPipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    inBufferSize: 0,
                    outBufferSize: 0,
                    pipeSecurity: CreateCurrentUserOnlyPipeSecurity());

                try
                {
                    await pipeStream.WaitForConnectionAsync(serverCt).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    pipeStream.Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger(typeof(NamedPipeHost))
                        .LogError(ex, "Error waiting for pipe connection");
                    pipeStream.Dispose();
                    await Task.Delay(ConnectionRetryDelayMs, serverCt).ConfigureAwait(false);
                    continue;
                }

                // 新しい接続を処理
                var connectionLogger = loggerFactory.CreateLogger<NamedPipeConnection>();
                var connection = new NamedPipeConnection(pipeStream, connectionLogger, serverCts);

                // ConcurrentDictionary に追加（thread-safe）
                connections.TryAdd(connection.ConnectionId, connection);

                // 接続を非同期で処理
                _ = HandleConnectionAsync(connection, connections, parentServiceProvider, serverCt);
            }
        }
        finally
        {
            // すべての接続を閉じる
            foreach (var conn in connections.Values)
            {
                try
                {
                    await conn.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // 無視
                }
            }

            connections.Clear();
        }
    }

    /// <summary>
    /// 単一の接続を処理する。
    /// </summary>
    private static async Task HandleConnectionAsync(
        NamedPipeConnection connection,
        ConcurrentDictionary<Guid, NamedPipeConnection> connections,
        IServiceProvider parentServiceProvider,
        CancellationToken ct)
    {
        try
        {
            // 接続ごとにキャンセルトークンをリンク
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            await connection.RunAsync(parentServiceProvider, cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            parentServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(NamedPipeHost))
                .LogError(ex, "Error handling connection {ConnectionId}", connection.ConnectionId);
        }
        finally
        {
            // 接続完了後、リストから削除（メモリリーク防止）
            connections.TryRemove(connection.ConnectionId, out _);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 対話セッション判定を行い、NG なら stderr に既定のエラーフォーマットで出力する。
    /// OK なら観測値を info ログとして stderr に1行記録する。
    /// </summary>
    /// <returns>対話デスクトップ判定が OK なら <see langword="true" />、NG なら <see langword="false" />。</returns>
    private static bool EnsureInteractiveSession()
    {
        var probe = InteractiveSessionGuard.Probe();
        if (!probe.Ok)
        {
            Console.Error.WriteLine($"error {InteractiveSessionGuard.ErrorCode}");
            Console.Error.WriteLine($"message {probe.Message}");
            Console.Error.WriteLine("hint launch 'adact serve pipe' from the interactive logon session that owns the target GUI windows");
            return false;
        }

        Console.Error.WriteLine(
            $"info interactive session ok (SessionId={probe.SessionId}, WindowStation={probe.WindowStationName})");
        return true;
    }

    /// <summary>
    /// ロガーファクトリを作成する。
    /// </summary>
    private static ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.IncludeScopes = false;
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    /// <summary>
    /// 現在ユーザーのみが Named Pipe を利用できるように制限したセキュリティ設定を作成する。
    /// </summary>
    private static PipeSecurity CreateCurrentUserOnlyPipeSecurity()
    {
        var pipeSecurity = new PipeSecurity();
        pipeSecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        var currentUser = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("Failed to resolve current Windows user SID.");

        pipeSecurity.AddAccessRule(new PipeAccessRule(
            currentUser,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        return pipeSecurity;
    }

}
