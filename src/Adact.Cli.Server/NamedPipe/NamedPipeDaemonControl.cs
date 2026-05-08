using Adact.Mcp.Common;

namespace Adact.Cli.Server.NamedPipe;

/// <summary>
/// </summary>
internal sealed class NamedPipeDaemonControl : IDaemonControl
{
    private readonly CancellationTokenSource _serverCts;

    /// <summary>
    /// </summary>
    public bool IsSupported => true;

    /// <summary>
    /// </summary>
    public NamedPipeDaemonControl(CancellationTokenSource serverCts)
    {
        _serverCts = serverCts ?? throw new ArgumentNullException(nameof(serverCts));
    }

    /// <summary>
    /// </summary>
    public Task StopAsync(CancellationToken ct)
    {
        _serverCts.Cancel();
        return Task.CompletedTask;
    }
}
