using System.Diagnostics;

namespace Adact.Cli.Tests;

/// <summary>
/// adact.exe をサブプロセス起動するヘルパー。
/// AssemblyName=adact なので、Adact.Cli の出力ディレクトリに <c>adact.exe</c> (apphost) が存在する。
/// </summary>
internal sealed record CliResult(int ExitCode, string Stdout, string Stderr);

internal static class CliProcess
{
    public static string ExePath { get; } = ResolveExePath();

    private static string ResolveExePath()
    {
        // typeof(Adact.Cli.Program) は internal だが Adact.Cli の InternalsVisibleTo で参照可能。
        var dllDir = Path.GetDirectoryName(typeof(Adact.Cli.Program).Assembly.Location)
            ?? throw new InvalidOperationException("Failed to determine Adact.Cli output directory.");
        var exe = Path.Combine(dllDir, "adact.exe");
        if (!File.Exists(exe))
        {
            throw new FileNotFoundException(
                $"adact.exe not found at '{exe}'. Ensure Adact.Cli builds an apphost (.exe).", exe);
        }
        return exe;
    }

    /// <summary>
    /// adact.exe を起動して stdout/stderr/exit code を返す。
    /// </summary>
    public static CliResult Run(
        string arguments,
        string? workingDirectory = null,
        TimeSpan? timeout = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var to = timeout ?? TimeSpan.FromSeconds(30);
        var psi = new ProcessStartInfo
        {
            FileName = ExePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(ExePath)!,
        };

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                if (value is null)
                {
                    psi.Environment.Remove(key);
                }
                else
                {
                    psi.Environment[key] = value;
                }
            }
        }

        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start adact.exe.");

        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();

        if (!p.WaitForExit((int)to.TotalMilliseconds))
        {
            try { p.Kill(entireProcessTree: true); } catch { }
            try { p.WaitForExit(2000); } catch { }
            var stdoutPartial = stdoutTask.IsCompletedSuccessfully ? stdoutTask.Result : "";
            var stderrPartial = stderrTask.IsCompletedSuccessfully ? stderrTask.Result : "";
            throw new TimeoutException(
                $"adact.exe did not exit within {to.TotalSeconds:F0}s. args=[{arguments}]\n" +
                $"stdout: {stdoutPartial}\nstderr: {stderrPartial}");
        }

        Task.WaitAll(stdoutTask, stderrTask);
        return new CliResult(p.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    /// <summary>
    /// <c>--server &lt;baseUrl&gt;</c> を末尾に付与して実行する。
    /// </summary>
    public static CliResult RunWithServer(
        string arguments,
        string baseUrl,
        string? workingDirectory = null,
        TimeSpan? timeout = null)
    {
        return Run($"{arguments} --server {baseUrl}", workingDirectory, timeout);
    }
}
