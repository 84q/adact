namespace Adact.Mcp.Common;

/// <summary>
/// daemon プロセス全体の停止を抽象化するインタフェース。HTTP モードでは
/// <c>IHostApplicationLifetime.StopApplication()</c> を呼び、stdio モードでは未対応。
/// 詳細は discussion/009_Phase5設計.md §4.5 参照。
/// </summary>
public interface IDaemonControl
{
    /// <summary>このモードで <c>adact_daemon_stop</c> が機能するか。stdio モードでは false。</summary>
    bool IsSupported { get; }

    /// <summary>
    /// daemon の HTTP listener を停止し graceful shutdown を要求する。
    /// stdio モードでは <see cref="InvalidOperationException"/> を throw する。
    /// </summary>
    /// <param name="ct">停止処理をキャンセルするためのトークン。</param>
    /// <exception cref="InvalidOperationException">stdio モードで呼び出された場合。</exception>
    Task StopAsync(CancellationToken ct);
}
