using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Xunit;

namespace Adact.Cli.Tests;

/// <summary>
/// <c>adact.exe serve</c> をサブプロセスとして一度だけ起動し、test collection 全体で共有する fixture。
/// ephemeral port (TcpListener.Start(0)) を OS から確保し、HEAD リクエストで起動完了をポーリングする。
/// Dispose 時は <see cref="Process.Kill()"/> でプロセスを終了する。
/// </summary>
/// <remarks>
/// 本 fixture の前提: <c>xunit.runner.json</c> で <c>parallelizeAssembly: false</c> が設定されていること。
/// 同一テストアセンブリ内では <c>[CollectionDefinition("AdactCli", DisableParallelization = true)]</c> によって
/// daemon を共有する全テストが直列化される。並列実行する場合は ephemeral port のリトライ実装が別途必要になる。
/// If <c>ADACT_SERVER_URL</c> is set, the fixture uses that external daemon and leaves its lifecycle to the caller.
/// </remarks>
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

    /// <summary>fixture が確保した ephemeral port。</summary>
    public int Port { get; private set; }

    /// <summary>起動した daemon の MCP エンドポイント (http://127.0.0.1:&lt;port&gt;/mcp)。</summary>
    public string BaseUrl { get; private set; } = null!;

    /// <summary>
    /// サブプロセスとして <c>adact serve</c> を起動し、HTTP ready 状態になるまでポーリングする。
    /// </summary>
    /// <returns>起動完了タスク。</returns>
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

        // 出力を読み続けないと PIPE が詰まるため、stdout/stderr を非同期に StringBuilder へ蓄積する。
        // 蓄積した内容は WaitForReadyAsync タイムアウト時の例外メッセージや
        // DisposeAsync の診断ログに利用する。
        _stdoutPump = PumpAsync(_serveProcess.StandardOutput, _stdout);
        _stderrPump = PumpAsync(_serveProcess.StandardError, _stderr);

        try
        {
            await WaitForReadyAsync(BaseUrl, StartupTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            // 起動失敗時は子プロセスを終了させ、蓄積したログをメッセージに含めて再 throw する。
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

    /// <summary>
    /// サブプロセスを終了し、診断ログを stderr へ出力する。
    /// HTTP モードの daemon は --server 指定で daemon-stop すると LOCAL_ONLY エラーになるため、
    /// 直接 Kill を使用する。
    /// </summary>
    /// <returns>解放完了タスク。</returns>
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

        // pump task の収束を待ち、stdout/stderr を確定させる。
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

        // xUnit はテスト実行終了後にも Console.Error への出力を診断として表示する。
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
            // プロセス終了時の読み取り失敗は許容。
        }
    }

    private static int GetFreePort()
    {
        // using で確実に Stop() し、bind を OS に返す。
        // parallelizeAssembly:false 前提なので競合は発生しないが、安全側に倒す。
        using var listener = new TcpListenerHandle(IPAddress.Loopback, 0);
        return listener.Port;
    }

    internal static string? GetExternalServerUrl()
    {
        var value = Environment.GetEnvironmentVariable(ServerUrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value)) return null;

        var url = value.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"{ServerUrlEnvironmentVariable} must be an absolute http(s) URL, e.g. http://127.0.0.1:41300/mcp.");
        }

        return uri.ToString();
    }

    private sealed class TcpListenerHandle : IDisposable
    {
        private readonly TcpListener _listener;
        public int Port { get; }

        public TcpListenerHandle(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

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
                // どんな HTTP ステータスでも (405 等) Kestrel が応答していれば ready。
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

/// <summary>
/// L4 Smoke / L5 E2E の CLI テストをまとめて直列に実行する collection。
/// daemon サブプロセスを共有しつつ、UIA を伴うテストを並列にしないために DisableParallelization を有効化する。
/// </summary>
[CollectionDefinition("AdactCli", DisableParallelization = true)]
public sealed class AdactCliCollection : ICollectionFixture<AdactDaemonFixture>
{
}
