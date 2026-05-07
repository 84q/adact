namespace Adact.Engine;

/// <summary>
/// kill 操作で実際に使われた終了手段を示す列挙。
/// </summary>
public enum KillMethod
{
    /// <summary>WM_CLOSE でプロセスが正常終了した。</summary>
    Graceful,

    /// <summary>--force 指定により Process.Kill で即時強制終了した。</summary>
    Forced,

    /// <summary>WM_CLOSE 後タイムアウトし Process.Kill でフォールバック終了した。</summary>
    ForcedAfterTimeout,
}
