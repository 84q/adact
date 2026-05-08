using System.Diagnostics;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Daemon;

/// <summary>
/// </summary>
internal static class DaemonSpawner
{
    private const int DefaultTimeoutMs = 30000;

    /// <summary>
    /// </summary>
    internal static Func<NamedPipeEndPoint, int, CancellationToken, Task<bool>> IsServerRunningAsync { get; set; }
        = NamedPipeMcpClient.IsServerRunningAsync;

    /// <summary>
    /// </summary>
    internal static Func<NamedPipeEndPoint, int, CancellationToken, Task<bool>> SpawnServerAsync { get; set; }
        = SpawnServerAsyncImpl;

    /// <summary>
    /// </summary>
    public static async Task<bool> EnsureServerRunningAsync(CancellationToken ct = default)
    {
        var endpoint = NamedPipeEndPoint.FromWorkspacePath(NamedPipeEndPoint.ResolveWorkspacePath());

        var pipeExists = await IsServerRunningAsync(endpoint, 100, ct).ConfigureAwait(false);

        if (pipeExists)
        {
            if (await IsServerRunningAsync(endpoint, 500, ct).ConfigureAwait(false))
            {
                return true;
            }

        }

        return await SpawnServerAsync(endpoint, DefaultTimeoutMs, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// </summary>
    private static async Task<bool> SpawnServerAsyncImpl(NamedPipeEndPoint endpoint, int timeoutMs, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = GetAdactExecutablePath(),
            Arguments = "serve pipe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        Process? process = null;
        try
        {
            process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            CliError.Write(
                ErrorCodes.InternalError,
                $"Failed to start adact serve pipe: {ex.Message}");
            return false;
        }

        var tcs = new TaskCompletionSource<bool>();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);

        void OnOutputDataReceived(object? _, DataReceivedEventArgs e)
        {
            if (e.Data?.Contains("Daemon listening on") == true)
            {
                tcs.TrySetResult(true);
            }
        }

        process.OutputDataReceived += OnOutputDataReceived;

        try
        {
            process.BeginOutputReadLine();

            try
            {
                // wait for startup completion or timeout
                await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs, cts.Token)).ConfigureAwait(false);

                if (tcs.Task.IsCompletedSuccessfully && tcs.Task.Result)
                {
                    // once the startup message is observed, verify the connection
                    await Task.Delay(500, ct).ConfigureAwait(false); // wait briefly to stabilize
                    return await NamedPipeMcpClient.IsServerRunningAsync(endpoint, 2000, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                process.CancelOutputRead();
                process.OutputDataReceived -= OnOutputDataReceived;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
            }

            return false;
        }
        finally
        {
            process.Dispose();
        }
    }

    /// <summary>
    /// </summary>
    private static string GetAdactExecutablePath()
    {
        var currentProcess = Process.GetCurrentProcess();
        return currentProcess.MainModule?.FileName ?? "adact";
    }
}
