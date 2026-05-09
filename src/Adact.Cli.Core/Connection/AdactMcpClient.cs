using Microsoft.Extensions.Logging;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Adact.Cli.Connection;

/// <summary>
/// </summary>
internal sealed class AdactMcpClient : IAdactMcpClient, IAsyncDisposable
{
    private readonly McpClient _client;

    public ServerEndpoint Endpoint { get; }

    private AdactMcpClient(McpClient client, ServerEndpoint endpoint)
    {
        _client = client;
        Endpoint = endpoint;
    }

    /// <summary>
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
    /// Calls an MCP tool over the HTTP transport.
    /// </summary>
    /// <param name="name">The MCP tool name.</param>
    /// <param name="arguments">The optional tool arguments.</param>
    /// <param name="cancellationToken">cancellation token。</param>
    /// <returns>MCP <see cref="CallToolResult"/>。</returns>
    public ValueTask<CallToolResult> CallToolAsync(
        string name,
        IReadOnlyDictionary<string, object?>? arguments,
        CancellationToken cancellationToken)
    {
        return _client.CallToolAsync(name, arguments, cancellationToken: cancellationToken);
    }

    public ValueTask DisposeAsync() => _client.DisposeAsync();
}
