using Adact.Mcp.Common;

namespace Adact.Mcp.Stdio;

/// <summary>
/// stdio モードでの <see cref="IDaemonControl"/> 実装。<c>daemon_stop</c> は非対応。
/// </summary>
internal sealed class StdioDaemonControl : IDaemonControl
{
  public bool IsSupported => false;

  public Task StopAsync(CancellationToken ct)
      => throw new InvalidOperationException("daemon_stop is not supported in stdio mode.");
}
