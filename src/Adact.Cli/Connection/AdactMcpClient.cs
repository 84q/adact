using Microsoft.Extensions.Logging;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Adact.Cli.Connection;

/// <summary>
/// HTTP MCP daemon に接続する <see cref="McpClient"/> のラッパー。
/// 各 CLI コマンドは <see cref="ConnectAsync"/> でクライアントを作成し、
/// <see cref="CallToolAsync"/> 経由で tool を呼び出す。
/// </summary>
internal sealed class AdactMcpClient : IAsyncDisposable
{
    private readonly McpClient _client;

    /// <summary>接続先 endpoint (URL / localhost 判定)。</summary>
    public ServerEndpoint Endpoint { get; }

    /// <summary>コンストラクタ。<see cref="ConnectAsync"/> のみから生成される。</summary>
    /// <param name="client">接続済みの <see cref="McpClient"/>。</param>
    /// <param name="endpoint">接続先 endpoint。</param>
    private AdactMcpClient(McpClient client, ServerEndpoint endpoint)
    {
        _client = client;
        Endpoint = endpoint;
    }

    /// <summary>
    /// HTTP (Streamable) transport で daemon に接続する。
    /// </summary>
    /// <param name="endpoint">接続先。</param>
    /// <param name="loggerFactory">クライアント内部ログ用。指定しない場合は null 可。</param>
    /// <param name="cancellationToken">接続を中断するための cancellation token。</param>
    /// <returns>接続済みの <see cref="AdactMcpClient"/>。</returns>
    public static async Task<AdactMcpClient> ConnectAsync(
        ServerEndpoint endpoint,
        ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = endpoint.Url,
            TransportMode = HttpTransportMode.StreamableHttp,
            Name = "adact-cli",
        });

        var client = await McpClient.CreateAsync(
            transport,
            loggerFactory: loggerFactory,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new AdactMcpClient(client, endpoint);
    }

    /// <summary>
    /// 指定 tool を呼び出す。<paramref name="arguments"/> が null なら引数なしで呼ぶ。
    /// </summary>
    /// <param name="name">tool 名 (例: <c>windows_attach</c>)。</param>
    /// <param name="arguments">tool に渡すキーバリューペア。</param>
    /// <param name="cancellationToken">cancellation token。</param>
    /// <returns>MCP <see cref="CallToolResult"/>。</returns>
    public ValueTask<CallToolResult> CallToolAsync(
        string name,
        IReadOnlyDictionary<string, object?>? arguments,
        CancellationToken cancellationToken)
    {
        return _client.CallToolAsync(name, arguments, cancellationToken: cancellationToken);
    }

    /// <summary>内部 <see cref="McpClient"/> を非同期に解放する。</summary>
    /// <returns>解放処理を表す <see cref="ValueTask"/>。</returns>
    public ValueTask DisposeAsync() => _client.DisposeAsync();
}
