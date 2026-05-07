using Adact.Mcp.Common;

using Microsoft.Extensions.Hosting;

namespace Adact.Cli.Server;

/// <summary>
/// HTTP モードでの <see cref="IDaemonControl"/> 実装。<see cref="IHostApplicationLifetime"/> 経由で
/// graceful shutdown を要求する。
/// </summary>
internal sealed class HttpDaemonControl : IDaemonControl
{
    /// <summary>HTTP ホストの停止を要求するために用いる ASP.NET Core の lifetime。</summary>
    private readonly IHostApplicationLifetime _lifetime;

    /// <summary>
    /// DI 経由で <see cref="IHostApplicationLifetime"/> を受け取り、HTTP モード用の daemon 制御を初期化する。
    /// </summary>
    /// <param name="lifetime">graceful shutdown 要求の発行先となるホスト lifetime。</param>
    public HttpDaemonControl(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    /// <summary>
    /// HTTP モードでは常に daemon 停止操作 (<c>adact_daemon_stop</c> ツール) をサポートするため <see langword="true"/> を返す。
    /// </summary>
    public bool IsSupported => true;

    /// <summary>
    /// <see cref="IHostApplicationLifetime.StopApplication"/> を呼び出して HTTP ホストの graceful shutdown を要求する。
    /// </summary>
    /// <param name="ct">キャンセルトークン。<see cref="IHostApplicationLifetime.StopApplication"/> はキャンセルをサポートしないため未使用。</param>
    /// <returns>停止要求の発行のみを表す、即時完了する <see cref="Task"/>。実際の停止完了は ASP.NET Core 側で進行する。</returns>
    public Task StopAsync(CancellationToken ct)
    {
        _ = ct; // StopApplication() does not support cancellation
        _lifetime.StopApplication();
        return Task.CompletedTask;
    }
}
