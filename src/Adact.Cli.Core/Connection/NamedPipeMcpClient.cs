using System.IO.Pipes;

using Microsoft.Extensions.Logging;

using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Adact.Cli.Connection;

/// <summary>
/// </summary>
internal sealed class NamedPipeMcpClient : IAdactMcpClient, IAsyncDisposable
{
    private const int ConnectTimeoutMilliseconds = 5000;
    private const int ServerProbeTimeoutMilliseconds = 1000;

    private readonly NamedPipeClientStream _pipeStream;
    private readonly StreamClientTransport _transport;
    private readonly McpClient _client;

    public NamedPipeEndPoint Endpoint { get; }

    /// <summary>
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
    /// </summary>
    public static async Task<NamedPipeMcpClient> ConnectAsync(
        NamedPipeEndPoint endpoint,
        ILoggerFactory? loggerFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var pipeName = endpoint.PipeName;
        if (pipeName.StartsWith(NamedPipeEndPoint.PipePrefix, StringComparison.Ordinal))
        {
            pipeName = pipeName[NamedPipeEndPoint.PipePrefix.Length..];
        }

        var pipeStream = new NamedPipeClientStream(
            ".", // server name (local machine)
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
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
    /// </summary>
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
    /// Calls an MCP tool over the named-pipe transport.
    /// </summary>
    /// <param name="name">The MCP tool name.</param>
    /// <param name="arguments">The optional tool arguments.</param>
    /// <param name="cancellationToken">cancellation token。</param>
    /// <returns>MCP <see cref="CallToolResult" />。</returns>
    public ValueTask<CallToolResult> CallToolAsync(
        string name,
        IReadOnlyDictionary<string, object?>? arguments,
        CancellationToken cancellationToken)
    {
        return _client.CallToolAsync(name, arguments, null, null, cancellationToken);
    }

    /// <remarks>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
        await _pipeStream.DisposeAsync().ConfigureAwait(false);
    }

    private static string GetAssemblyVersion()
    {
        var v = typeof(NamedPipeMcpClient).Assembly.GetName().Version;
        return v?.ToString() ?? "0.0.0";
    }
}
