using System.Diagnostics;

using Adact.Engine.Exceptions;

namespace Adact.Engine;

public sealed partial class UiaEngine
{
    private static readonly TimeSpan WaitForWindowPollInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Waits for a top-level window that matches the query.
    /// </summary>
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
    /// </summary>
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
