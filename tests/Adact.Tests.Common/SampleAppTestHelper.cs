using System.Diagnostics;

using Adact.Engine;

namespace Adact.Tests.Common;

/// <summary>
/// SampleApp を使う実アプリ依存テスト向けの起動・停止・条件待機ヘルパー。
/// SampleApp の起動・停止・識別・条件待機を提供するヘルパー。
/// </summary>
public static class SampleAppTestHelper
{
    private const string ProcessName = "SampleApp";

    private const string WindowTitle = "ADACT SampleApp";

    /// <summary>
    /// 既存の SampleApp プロセスを掃除したうえで SampleApp を起動し、UIA から観測可能な状態になるまで待機する。
    /// </summary>
    /// <param name="timeout">タイムアウト。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>起動したプロセス。</returns>
    public static async Task<Process> StartFreshSampleAppAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        KillSampleAppProcesses();
        await WaitUntilAsync(
            () => Task.FromResult(Process.GetProcessesByName(ProcessName).Length == 0),
            TimeSpan.FromSeconds(5),
            "Existing SampleApp processes did not exit in time.",
            ct: ct).ConfigureAwait(false);

        var exePath = FindSampleAppPath();
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
        });
        if (process is null)
        {
            throw new InvalidOperationException($"Failed to start {exePath}.");
        }

        await WaitUntilAsync(
            () => Task.FromResult(IsSampleAppReady()),
            timeout,
            $"SampleApp did not become ready within {timeout}.",
            ct: ct).ConfigureAwait(false);

        return process;
    }

    /// <summary>
    /// 既知の SampleApp プロセスを best-effort で終了する。
    /// </summary>
    public static void KillSampleAppProcesses()
    {
        foreach (var process in Process.GetProcessesByName(ProcessName))
        {
            try
            {
                process.Kill();
                process.WaitForExit(2000);
            }
            catch
            {
            }
            finally { process.Dispose(); }
        }
    }

    /// <summary>
    /// <see cref="UiaEngine.ListWindowsAsync(CancellationToken)"/> 結果が SampleApp ウィンドウを指すかどうかを判定する。
    /// </summary>
    public static bool IsSampleAppWindow(WindowInfo info)
        => info.Title.Contains(WindowTitle, StringComparison.Ordinal)
            || info.ProcessName.Contains(ProcessName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 条件が満たされるまでポーリングし、タイムアウト時は明示例外を投げる。
    /// </summary>
    public static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        string failureMessage,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(150);
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (await condition().ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(interval, ct).ConfigureAwait(false);
        }

        throw new TimeoutException(failureMessage);
    }

    /// <summary>SampleApp.exe のパスを探す。</summary>
    private static string FindSampleAppPath()
    {
        // テストアセンブリの場所からソルーションルートを割り出し、SampleApp のビルド出力を探す
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && dir.GetFiles("adact.sln*").Length == 0)
            dir = dir.Parent;

        if (dir is null)
            throw new FileNotFoundException(
                "adact.sln not found. Cannot locate SampleApp.exe.");

        var exePath = Path.Combine(
            dir.FullName, "samples", "SampleApp", "bin", "Debug", "net10.0-windows", "SampleApp.exe");

        if (!File.Exists(exePath))
            throw new FileNotFoundException(
                $"SampleApp.exe not found at expected path: {exePath}. Build the solution first.");

        return exePath;
    }

    private static bool IsSampleAppReady()
    {
        foreach (var process in Process.GetProcessesByName(ProcessName))
        {
            try
            {
                process.Refresh();
                if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                {
                    return true;
                }
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return false;
    }
}
