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

    public ServerEndpoint Endpoint { get; }

    private AdactMcpClient(McpClient client, ServerEndpoint endpoint)
    {
        _client = client;
        Endpoint = endpoint;
    }

    /// <summary>
    /// HTTP (Streamable) transport で daemon に接続する。
    /// </summary>
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
    public ValueTask<CallToolResult> CallToolAsync(
        string name,
        IReadOnlyDictionary<string, object?>? arguments,
        CancellationToken cancellationToken)
    {
        return _client.CallToolAsync(name, arguments, cancellationToken: cancellationToken);
    }

    public ValueTask DisposeAsync() => _client.DisposeAsync();
}
