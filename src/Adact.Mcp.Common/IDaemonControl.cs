namespace Adact.Mcp.Common;

/// <summary>
/// Controls daemon lifecycle operations.
/// </summary>
public interface IDaemonControl
{
    /// <summary>
    /// Gets a value indicating whether daemon stop operations are supported.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Stops the daemon asynchronously.
    /// </summary>
    Task StopAsync(CancellationToken ct);
}
