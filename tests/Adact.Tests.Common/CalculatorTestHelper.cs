using System.Diagnostics;

using Adact.Engine;

namespace Adact.Tests.Common;

/// <summary>
/// 電卓を使う実アプリ依存テスト向けの起動・停止・条件待機ヘルパー。
/// </summary>
public static class CalculatorTestHelper
{
    private static readonly string[] CalculatorProcessNames = ["CalculatorApp", "calc"];

    /// <summary>
    /// 既存の電卓プロセスを掃除したうえで <c>calc.exe</c> を起動し、UIA から観測可能な状態になるまで待機する。
    /// </summary>
    public static async Task<Process> StartFreshCalculatorAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        KillCalculatorProcesses();
        await WaitUntilAsync(
            () => Task.FromResult(CalculatorProcessNames.All(name => Process.GetProcessesByName(name).Length == 0)),
            TimeSpan.FromSeconds(5),
            "Existing calculator processes did not exit in time.",
            ct: ct).ConfigureAwait(false);

        var process = Process.Start(new ProcessStartInfo { FileName = "calc.exe", UseShellExecute = true });
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start calc.exe: Process.Start returned null.");
        }

        await WaitUntilAsync(
            () => Task.FromResult(IsCalculatorReady()),
            timeout,
            $"CalculatorApp did not become ready within {timeout}.",
            ct: ct).ConfigureAwait(false);

        return process;
    }

    /// <summary>
    /// 既知の電卓プロセスを best-effort で終了する。
    /// </summary>
    public static void KillCalculatorProcesses()
    {
        foreach (var name in CalculatorProcessNames)
        {
            foreach (var process in Process.GetProcessesByName(name))
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
    }

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

    /// <summary>
    /// <see cref="UiaEngine.ListWindowsAsync(CancellationToken)"/> 結果が電卓ウィンドウを指すかどうかを判定する。
    /// </summary>
    public static bool IsCalculatorWindow(WindowInfo info)
        => info.Title.Contains("電卓", StringComparison.Ordinal)
            || info.Title.Contains("Calculator", StringComparison.OrdinalIgnoreCase)
            || info.ProcessName.Contains("Calculator", StringComparison.OrdinalIgnoreCase);

    private static bool IsCalculatorReady()
    {
        foreach (var process in Process.GetProcessesByName("CalculatorApp"))
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
