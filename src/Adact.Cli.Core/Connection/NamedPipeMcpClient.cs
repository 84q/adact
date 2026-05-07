using System.IO.Pipes;

using Microsoft.Extensions.Logging;

using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Adact.Cli.Connection;

/// <summary>
/// Named Pipe 経由で MCP サーバーに接続するクライアント。
/// StreamClientTransport を使用して実装。
/// 設計: discussion/033_NamedPipe_HTTP_統合設計.md §2.2
/// </summary>
internal sealed class NamedPipeMcpClient : IAdactMcpClient, IAsyncDisposable
{
    private const int ConnectTimeoutMilliseconds = 5000;
    private const int ServerProbeTimeoutMilliseconds = 1000;

    private readonly NamedPipeClientStream _pipeStream;
    private readonly StreamClientTransport _transport;
    private readonly McpClient _client;

    /// <summary>接続先の Named Pipe エンドポイント。</summary>
    public NamedPipeEndPoint Endpoint { get; }

    /// <summary>
    /// コンストラクタ。<see cref="ConnectAsync" /> のみから生成される。
    /// </summary>
    private NamedPipeMcpClient(
        NamedPipeClientStream pipeStream,
        StreamClientTransport transport,
        McpClient client,
        NamedPipeEndPoint endpoint)
    {
        _pipeStream = pipeStream;
        _transport = transport;
        _client = client;
        Endpoint = endpoint;
    }

    /// <summary>
    /// Named Pipe 経由で MCP サーバーに接続する。
    /// </summary>
    /// <param name="endpoint">接続先の Named Pipe エンドポイント。</param>
    /// <param name="loggerFactory">クライアント内部ログ用。指定しない場合は null 可。</param>
    /// <param name="cancellationToken">接続を中断するための cancellation token。</param>
    /// <returns>接続済みの <see cref="NamedPipeMcpClient" />。</returns>
    /// <exception cref="TimeoutException">接続タイムアウト時。</exception>
    /// <exception cref="IOException">接続失敗時。</exception>
    public static async Task<NamedPipeMcpClient> ConnectAsync(
        NamedPipeEndPoint endpoint,
        ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        // Named Pipe クライアントストリームを作成
        // PipeName から "\\.\pipe\" プレフィックスを除去してサーバー名を抽出
        var pipeName = endpoint.PipeName;
        if (pipeName.StartsWith(NamedPipeEndPoint.PipePrefix, StringComparison.Ordinal))
        {
            pipeName = pipeName[NamedPipeEndPoint.PipePrefix.Length..];
        }

        var pipeStream = new NamedPipeClientStream(
            ".", // サーバー名（ローカルマシン）
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
            // 接続を試行（タイムアウト: 5秒）
            await pipeStream.ConnectAsync(ConnectTimeoutMilliseconds, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            pipeStream.Dispose();
            throw new TimeoutException(
                $"Failed to connect to named pipe '{endpoint.PipeName}' within 5 seconds. " +
                "Ensure 'adact serve pipe' is running.");
        }
        catch (Exception ex)
        {
            pipeStream.Dispose();
            throw new IOException(
                $"Failed to connect to named pipe '{endpoint.PipeName}': {ex.Message}", ex);
        }

        // StreamClientTransport を使用して MCP クライアントを作成
        var transport = new StreamClientTransport(pipeStream, pipeStream);
        var client = await McpClient.CreateAsync(
            transport,
            new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "adact-cli",
                    Version = GetAssemblyVersion(),
                },
            },
            loggerFactory,
            cancellationToken).ConfigureAwait(false);

        return new NamedPipeMcpClient(pipeStream, transport, client, endpoint);
    }

    /// <summary>
    /// 指定されたパイプ名に接続できるか確認する（サーバーが起動しているか確認）。
    /// </summary>
    /// <param name="endpoint">確認する Named Pipe エンドポイント。</param>
    /// <param name="timeoutMs">タイムアウト（ミリ秒）。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>接続可能な場合は true、それ以外は false。</returns>
    public static async Task<bool> IsServerRunningAsync(
        NamedPipeEndPoint endpoint,
        int timeoutMs = ServerProbeTimeoutMilliseconds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var pipeName = endpoint.PipeName;
        if (pipeName.StartsWith(NamedPipeEndPoint.PipePrefix, StringComparison.Ordinal))
        {
            pipeName = pipeName[NamedPipeEndPoint.PipePrefix.Length..];
        }

        try
        {
            using var pipeStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeStream.ConnectAsync(timeoutMs, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 指定 tool を呼び出す。<paramref name="arguments" /> が null なら引数なしで呼ぶ。
    /// </summary>
    /// <param name="name">tool 名 (例: <c>adact_attach</c>)。</param>
    /// <param name="arguments">tool に渡すキーバリューペア。</param>
    /// <param name="cancellationToken">cancellation token。</param>
    /// <returns>MCP <see cref="CallToolResult" />。</returns>
    public ValueTask<CallToolResult> CallToolAsync(
        string name,
        IReadOnlyDictionary<string, object?>? arguments,
        CancellationToken cancellationToken)
    {
        return _client.CallToolAsync(name, arguments, null, null, cancellationToken);
    }

    /// <summary>内部リソースを非同期に解放する。</summary>
    /// <remarks>
    /// NOTE: StreamClientTransport は IDisposable/IAsyncDisposable を実装していないため、
    /// 明示的な破棄は行えない。ただし、NamedPipeClientStream (_pipeStream) を破棄することで、
    ///  underlying の pipe が閉じられ、StreamClientTransport も実質的に無力化される。
    /// Server プロセス終了時には OS が pipe をクリーンアップし、client 側も自動的に切断される。
    /// StreamClientTransport インスタンス自体はマネージドオブジェクトであり、GC で回収される。
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
        await _pipeStream.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>現在のアセンブリのバージョンを文字列化して返す。</summary>
    private static string GetAssemblyVersion()
    {
        var v = typeof(NamedPipeMcpClient).Assembly.GetName().Version;
        return v?.ToString() ?? "0.0.0";
    }
}
