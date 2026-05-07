using Adact.Mcp.Common;

namespace Adact.Cli.Server.NamedPipe;

/// <summary>
/// Named Pipe モードでの <see cref="IDaemonControl"/> 実装。
/// サーバープロセス全体を停止する。
/// </summary>
internal sealed class NamedPipeDaemonControl : IDaemonControl
{
    private readonly CancellationTokenSource _serverCts;

    /// <summary>
    /// Named Pipe モードでは adact_daemon_stop をサポートするため true。
    /// </summary>
    public bool IsSupported => true;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="serverCts">サーバー停止用の CancellationTokenSource。</param>
    public NamedPipeDaemonControl(CancellationTokenSource serverCts)
    {
        _serverCts = serverCts ?? throw new ArgumentNullException(nameof(serverCts));
    }

    /// <summary>
    /// Named Pipe サーバーを停止する。
    /// </summary>
    public Task StopAsync(CancellationToken ct)
    {
        _serverCts.Cancel();
        return Task.CompletedTask;
    }
}
