using Adact.Mcp.Common;

namespace Adact.Mcp.Stdio;

/// <summary>
/// stdio モードでの <see cref="IDaemonControl"/> 実装。<c>daemon_stop</c> は非対応。
/// </summary>
internal sealed class StdioDaemonControl : IDaemonControl
{
    /// <summary>
    /// stdio モードでは <c>daemon_stop</c> をサポートしないため常に <see langword="false"/>。
    /// </summary>
    public bool IsSupported => false;

    /// <summary>
    /// stdio モードでは停止対象の HTTP listener が存在しないため、常に例外を投げる。
    /// </summary>
    /// <param name="ct">使用しない。インタフェース整合のためのキャンセルトークン。</param>
    /// <exception cref="InvalidOperationException">stdio モードで呼び出された場合に必ず発生。</exception>
    public Task StopAsync(CancellationToken ct)
        => throw new InvalidOperationException("daemon_stop is not supported in stdio mode.");
}
