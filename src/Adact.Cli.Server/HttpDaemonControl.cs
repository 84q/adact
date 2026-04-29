using Adact.Mcp.Common;

using Microsoft.Extensions.Hosting;

namespace Adact.Cli.Server;

/// <summary>
/// HTTP モードでの <see cref="IDaemonControl"/> 実装。<see cref="IHostApplicationLifetime"/> 経由で
/// graceful shutdown を要求する。
/// </summary>
internal sealed class HttpDaemonControl : IDaemonControl
{
    private readonly IHostApplicationLifetime _lifetime;

    public HttpDaemonControl(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    public bool IsSupported => true;

    public Task StopAsync(CancellationToken ct)
    {
        _ = ct; // StopApplication() does not support cancellation
        _lifetime.StopApplication();
        return Task.CompletedTask;
    }
}
