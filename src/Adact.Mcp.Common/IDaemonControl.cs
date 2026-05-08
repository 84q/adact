namespace Adact.Mcp.Common;

/// <summary>
/// Controls daemon lifecycle operations.
/// </summary>
public interface IDaemonControl
{
    bool IsSupported { get; }

    /// <summary>
    /// Stops the daemon asynchronously.
    /// </summary>
    Task StopAsync(CancellationToken ct);
}
