using System.Diagnostics;

using Adact.Engine.Exceptions;

namespace Adact.Engine;

public sealed partial class UiaEngine
{
    /// <summary>UiaEngine.WaitForWindow の内部ポーリング間隔 (設計 022 §13)。</summary>
    private static readonly TimeSpan WaitForWindowPollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// 検索条件にマッチする top-level window が出現するまで待機する (設計 022 §6 / §7)。
    /// attach は行わない。条件に最初にマッチした window を返す。
    /// </summary>
    /// <param name="query">window 検索条件。少なくとも 1 つのフィールド必須。</param>
    /// <param name="timeout">待機タイムアウト。<see cref="TimeSpan.Zero"/> 以下は <see cref="ArgumentOutOfRangeException"/>。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>マッチした window の <see cref="WindowInfo"/>。</returns>
    /// <exception cref="ObjectDisposedException">本 Engine が Dispose 済みの場合。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> が null。</exception>
    /// <exception cref="ArgumentException">クエリが空。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> が 0 以下。</exception>
    /// <exception cref="WaitTimeoutException">タイムアウト内にマッチする window が現れなかった。</exception>
    public async Task<WindowInfo> WaitForWindowAsync(
        WindowSearchQuery query,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query);
        if (!query.HasAnyCondition)
            throw new ArgumentException("Query must specify at least one condition.", nameof(query));
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be positive.");
        ct.ThrowIfCancellationRequested();

        var deadline = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var found = await RunSerializedAsync(c =>
            {
                c.ThrowIfCancellationRequested();
                var windows = ListWindowsCore();
                foreach (var w in windows)
                {
                    var exePath = TryGetExecutablePath(w.ProcessId);
                    if (query.Matches(w, exePath))
                    {
                        return Task.FromResult<WindowInfo?>(w);
                    }
                }
                return Task.FromResult<WindowInfo?>(null);
            }, ct).ConfigureAwait(false);

            if (found is not null) return found;

            if (Stopwatch.GetTimestamp() >= deadline)
            {
                throw new WaitTimeoutException(
                    $"wait-for-window did not observe a matching window within {(int)timeout.TotalMilliseconds}ms.");
            }

            await Task.Delay(WaitForWindowPollInterval, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 指定 PID のプロセスのフルパスを取得する。アクセス拒否や終了済み等のエラーは null を返す。
    /// </summary>
    /// <param name="pid">プロセス ID。</param>
    /// <returns>実行ファイルのフルパス、取得不能なら null。</returns>
    private static string? TryGetExecutablePath(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            return p.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }
}
