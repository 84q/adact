using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Adact.Tests.Common;

using Xunit;

namespace Adact.Cli.Tests;

/// <summary>Provides a shared fixture for tests.</summary>
public sealed class AdactDaemonFixture : IAsyncLifetime
{
    internal const string ServerUrlEnvironmentVariable = "ADACT_SERVER_URL";

    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(10);

    private Process? _serveProcess;
    private bool _usesExternalServer;
    private readonly StringBuilder _stdout = new();
    private readonly StringBuilder _stderr = new();
    private readonly StringBuilder _diagnostics = new();
    private Task? _stdoutPump;
    private Task? _stderrPump;

    /// <summary>Gets or sets the Port value.</summary>
    public int Port { get; private set; }

    /// <summary>Gets or sets the Base Url value.</summary>
    public string BaseUrl { get; private set; } = null!;

    /// <summary>Initializes the fixture.</summary>
    public async Task InitializeAsync()
    {
        var externalBaseUrl = GetExternalServerUrl();
        if (externalBaseUrl is not null)
        {
            BaseUrl = externalBaseUrl;
            _usesExternalServer = true;
            await WaitForReadyAsync(BaseUrl, StartupTimeout).ConfigureAwait(false);
            return;
        }

        Port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{Port}/mcp";

        var psi = new ProcessStartInfo
        {
            FileName = CliProcess.ExePath,
            Arguments = $"serve http --port {Port}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        _serveProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start 'adact serve' subprocess.");

        _stdoutPump = PumpAsync(_serveProcess.StandardOutput, _stdout);
        _stderrPump = PumpAsync(_serveProcess.StandardError, _stderr);

        try
        {
            await WaitForReadyAsync(BaseUrl, StartupTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            try { _serveProcess.Kill(entireProcessTree: true); } catch { }
            try { _serveProcess.WaitForExit(2000); } catch { }
            string capturedStdout, capturedStderr;
            lock (_stdout) capturedStdout = _stdout.ToString();
            lock (_stderr) capturedStderr = _stderr.ToString();
            throw new TimeoutException(
                ex.Message
                + $"\n--- serve stdout ---\n{capturedStdout}"
                + $"\n--- serve stderr ---\n{capturedStderr}",
                ex.InnerException);
        }
    }

    /// <summary>Releases resources.</summary>
    public async Task DisposeAsync()
    {
        if (_usesExternalServer) return;
        if (_serveProcess is null) return;

        try
        {
            if (!_serveProcess.HasExited)
            {
                try
                {
                    _serveProcess.Kill(entireProcessTree: true);
                    _diagnostics.AppendLine("Kill() called.");
                }
                catch (Exception ex)
                {
                    _diagnostics.AppendLine($"Kill threw: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _serveProcess.WaitForExit(5000);

            if (_serveProcess.HasExited)
            {
                _diagnostics.AppendLine($"serve exited with code {_serveProcess.ExitCode}.");
            }
            else
            {
                _diagnostics.AppendLine("serve still running after Kill.");
            }
        }
        catch (Exception ex)
        {
            _diagnostics.AppendLine($"WaitForExit/Kill threw: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            if (_stdoutPump is not null) await Task.WhenAny(_stdoutPump, Task.Delay(2000)).ConfigureAwait(false);
            if (_stderrPump is not null) await Task.WhenAny(_stderrPump, Task.Delay(2000)).ConfigureAwait(false);
        }
        catch { }

        try { _serveProcess.Dispose(); } catch { }
        _serveProcess = null;

        string finalStdout, finalStderr;
        lock (_stdout) finalStdout = _stdout.ToString();
        lock (_stderr) finalStderr = _stderr.ToString();

        Console.Error.WriteLine(
            $"[AdactDaemonFixture] {_diagnostics}"
            + $"--- serve stdout ---\n{finalStdout}"
            + $"--- serve stderr ---\n{finalStderr}");
    }

    private static async Task PumpAsync(System.IO.StreamReader reader, StringBuilder sink)
    {
        try
        {
            char[] buffer = new char[4096];
            int read;
            while ((read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                lock (sink) sink.Append(buffer, 0, read);
            }
        }
        catch
        {
        }
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListenerHandle(IPAddress.Loopback, 0);
        return listener.Port;
    }

    internal static string? GetExternalServerUrl()
        => GetExternalServerUrl(Environment.GetEnvironmentVariable);

    internal static string? GetExternalServerUrl(Func<string, string?> getEnvironmentVariable)
    {
        return ExternalServerHelper.GetExternalServerUri(getEnvironmentVariable)?.ToString();
    }

    private sealed class TcpListenerHandle : IDisposable
    {
        private readonly TcpListener _listener;
        /// <summary>Gets the Port value.</summary>
        public int Port { get; }

        /// <summary>Initializes a new instance of the Tcp Listener Handle class.</summary>
        public TcpListenerHandle(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        /// <summary>Releases resources.</summary>
        public void Dispose() => _listener.Stop();
    }

    private static async Task WaitForReadyAsync(string baseUrl, TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var sw = Stopwatch.StartNew();
        Exception? last = null;
        while (sw.Elapsed < timeout)
        {
            try
            {
                using var resp = await http.GetAsync(baseUrl).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(200).ConfigureAwait(false);
            }
        }
        throw new TimeoutException(
            $"'adact serve' did not become ready within {timeout.TotalSeconds:F0}s on {baseUrl}.",
            last);
    }
}

/// <summary>Defines a shared test collection.</summary>
[CollectionDefinition("AdactCli", DisableParallelization = true)]
public sealed class AdactCliCollection : ICollectionFixture<AdactDaemonFixture>
{
}
