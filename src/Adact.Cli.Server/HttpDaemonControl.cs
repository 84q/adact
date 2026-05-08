using Adact.Mcp.Common;

using Microsoft.Extensions.Hosting;

namespace Adact.Cli.Server;

/// <summary>
/// </summary>
internal sealed class HttpDaemonControl : IDaemonControl
{
    private readonly IHostApplicationLifetime _lifetime;

    /// <summary>
    /// </summary>
    public HttpDaemonControl(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    /// <summary>
    /// </summary>
    public bool IsSupported => true;

    /// <summary>
    /// </summary>
    public Task StopAsync(CancellationToken ct)
    {
        _ = ct; // StopApplication() does not support cancellation
        _lifetime.StopApplication();
        return Task.CompletedTask;
    }
}
