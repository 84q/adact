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
/// </summary>
internal sealed class NamedPipeConnection : IAsyncDisposable
{
    private readonly NamedPipeServerStream _pipeStream;
    private readonly ILogger<NamedPipeConnection> _logger;
    private readonly CancellationTokenSource _connectionCts;
    private readonly CancellationTokenSource _serverCts;
    private int _closed;
    private int _disposed;

    public bool IsConnected => _pipeStream.IsConnected && _closed == 0 && _disposed == 0;

    public Guid ConnectionId { get; } = Guid.NewGuid();

    /// <summary>
    /// </summary>
    public NamedPipeConnection(NamedPipeServerStream pipeStream, ILogger<NamedPipeConnection> logger, CancellationTokenSource serverCts)
    {
        _pipeStream = pipeStream ?? throw new ArgumentNullException(nameof(pipeStream));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionCts = new CancellationTokenSource();
        _serverCts = serverCts ?? throw new ArgumentNullException(nameof(serverCts));
    }

    /// <summary>
    /// </summary>
    public async Task RunAsync(IServiceProvider parentServices, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, nameof(NamedPipeConnection));

        try
        {
            _logger.LogDebug("Starting MCP server for connection {ConnectionId}", ConnectionId);

            var loggerFactory = parentServices.GetService<ILoggerFactory>()
                ?? LoggerFactory.Create(b => b.AddConsole());

            await using var transport = new StreamServerTransport(
                _pipeStream,
                _pipeStream,
                $"adact-namedpipe-{ConnectionId:N}",
                loggerFactory);

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

            var toolCollection = new McpServerPrimitiveCollection<McpServerTool>();
            var windowsTools = new WindowsTools(
                parentServices.GetRequiredService<SessionStore>(),
                parentServices.GetRequiredService<WindowRefStore>(),
                new NamedPipeDaemonControl(_serverCts),
                parentServices.GetRequiredService<ILogger<WindowsTools>>());

            var toolMethods = typeof(WindowsTools).GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(McpServerToolAttribute), false).Length > 0);

            foreach (var method in toolMethods)
            {
                var tool = McpServerTool.Create(method, windowsTools);
                toolCollection.Add(tool);
            }

            serverOptions.ToolCollection = toolCollection;

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

    private static string GetAssemblyVersion()
    {
        var v = typeof(NamedPipeConnection).Assembly.GetName().Version;
        return v?.ToString() ?? "0.0.0";
    }

    /// <summary>
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
