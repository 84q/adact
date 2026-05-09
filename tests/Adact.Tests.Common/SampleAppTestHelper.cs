using System.Diagnostics;

using Adact.Engine;

namespace Adact.Tests.Common;

/// <summary>Provides helper methods for tests.</summary>
public static class SampleAppTestHelper
{
    private const string ProcessName = "SampleApp";

    private const string WindowTitle = "ADACT SampleApp";

    /// <summary>Performs the Start Fresh Sample App Async operation.</summary>
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

    /// <summary>Performs the Kill Sample App Processes operation.</summary>
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

    /// <summary>Gets a value indicating whether Is Sample App Window.</summary>
    public static bool IsSampleAppWindow(WindowInfo info)
        => info.Title.Contains(WindowTitle, StringComparison.Ordinal)
            || info.ProcessName.Contains(ProcessName, StringComparison.OrdinalIgnoreCase);

    /// <summary>Waits for the Wait Until Async condition.</summary>
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

    private static string FindSampleAppPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && dir.GetFiles("adact.sln*").Length == 0)
            dir = dir.Parent;

        if (dir is null)
            throw new FileNotFoundException(
                "adact.sln not found. Cannot locate SampleApp.exe.");

        var exePath = Path.Combine(
            dir.FullName, "test-apps", "SampleApp", "bin", "Debug", "net10.0-windows", "SampleApp.exe");

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
