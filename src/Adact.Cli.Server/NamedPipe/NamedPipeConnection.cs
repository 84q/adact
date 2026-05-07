using System.IO.Pipes;
using System.Reflection;

using Adact.Engine;
using Adact.Mcp.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Cli.Server.NamedPipe;

/// <summary>
/// 単一の Named Pipe 接続を管理するクラス。
/// クライアント接続ごとにインスタンスが作成される。
/// StreamServerTransport を使用して MCP サーバーを提供する。
/// </summary>
internal sealed class NamedPipeConnection : IAsyncDisposable
{
    private readonly NamedPipeServerStream _pipeStream;
    private readonly ILogger<NamedPipeConnection> _logger;
    private readonly CancellationTokenSource _connectionCts;
    private readonly CancellationTokenSource _serverCts;
    private int _closed;
    private int _disposed;

    /// <summary>接続がアクティブかどうか。</summary>
    public bool IsConnected => _pipeStream.IsConnected && _closed == 0 && _disposed == 0;

    /// <summary>接続ID（デバッグ用）。</summary>
    public Guid ConnectionId { get; } = Guid.NewGuid();

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="pipeStream">Named Pipe サーバーストリーム。</param>
    /// <param name="logger">ロガー。</param>
    /// <param name="serverCts">サーバー停止用の CancellationTokenSource（adact_daemon_stop で使用）。</param>
    public NamedPipeConnection(NamedPipeServerStream pipeStream, ILogger<NamedPipeConnection> logger, CancellationTokenSource serverCts)
    {
        _pipeStream = pipeStream ?? throw new ArgumentNullException(nameof(pipeStream));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionCts = new CancellationTokenSource();
        _serverCts = serverCts ?? throw new ArgumentNullException(nameof(serverCts));
    }

    /// <summary>
    /// MCP サーバーを設定して接続を処理開始する。
    /// </summary>
    /// <param name="parentServices">親サービスプロバイダー。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    public async Task RunAsync(IServiceProvider parentServices, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, nameof(NamedPipeConnection));

        try
        {
            _logger.LogDebug("Starting MCP server for connection {ConnectionId}", ConnectionId);

            var loggerFactory = parentServices.GetService<ILoggerFactory>()
                ?? LoggerFactory.Create(b => b.AddConsole());

            // StreamServerTransport を作成
            await using var transport = new StreamServerTransport(
                _pipeStream,
                _pipeStream,
                $"adact-namedpipe-{ConnectionId:N}",
                loggerFactory);

            // MCP サーバーオプションを構築
            var serverOptions = new McpServerOptions
            {
                ServerInfo = new Implementation
                {
                    Name = "adact",
                    Version = GetAssemblyVersion(),
                },
                Capabilities = new ServerCapabilities
                {
                    Tools = new ToolsCapability(),
                },
            };

            // WindowsTools からツールを作成して登録
            var toolCollection = new McpServerPrimitiveCollection<McpServerTool>();
            var windowsTools = new WindowsTools(
                parentServices.GetRequiredService<SessionStore>(),
                parentServices.GetRequiredService<WindowRefStore>(),
                new NamedPipeDaemonControl(_serverCts),
                parentServices.GetRequiredService<ILogger<WindowsTools>>());

            // ツールメソッドをリフレクションで取得して登録
            var toolMethods = typeof(WindowsTools).GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(McpServerToolAttribute), false).Length > 0);

            foreach (var method in toolMethods)
            {
                var tool = McpServerTool.Create(method, windowsTools);
                toolCollection.Add(tool);
            }

            serverOptions.ToolCollection = toolCollection;

            // MCP サーバーを作成して実行
            await using var server = McpServer.Create(transport, serverOptions, loggerFactory, parentServices);
            await server.RunAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Connection {ConnectionId} handler completed", ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing connection {ConnectionId}", ConnectionId);
            throw;
        }
    }

    /// <summary>現在のアセンブリのバージョンを文字列化して返す。</summary>
    private static string GetAssemblyVersion()
    {
        var v = typeof(NamedPipeConnection).Assembly.GetName().Version;
        return v?.ToString() ?? "0.0.0";
    }

    /// <summary>
    /// 接続を閉じる。
    /// </summary>
    public async Task CloseAsync()
    {
        if (Interlocked.CompareExchange(ref _closed, 1, 0) != 0)
        {
            return;
        }

        _logger.LogDebug("Closing connection {ConnectionId}", ConnectionId);

        await _connectionCts.CancelAsync().ConfigureAwait(false);

        try
        {
            if (_pipeStream.IsConnected)
            {
                _pipeStream.Disconnect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disconnecting pipe for connection {ConnectionId}", ConnectionId);
        }
    }

    /// <summary>リソースを解放する。</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        await CloseAsync().ConfigureAwait(false);

        _connectionCts.Dispose();

        await _pipeStream.DisposeAsync().ConfigureAwait(false);

        _logger.LogDebug("Connection {ConnectionId} disposed", ConnectionId);
    }
}
