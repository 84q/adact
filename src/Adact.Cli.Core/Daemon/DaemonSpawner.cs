using System.Diagnostics;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Daemon;

/// <summary>
/// ADACT daemon を自動起動するクラス。
/// 設計 033 §5.2
/// </summary>
internal static class DaemonSpawner
{
    private const int DefaultTimeoutMs = 30000;

    /// <summary>
    /// サーバーが既に起動しているか確認する関数。
    /// テストでモック可能にするため internal static プロパティとして公開する。
    /// </summary>
    internal static Func<NamedPipeEndPoint, int, CancellationToken, Task<bool>> IsServerRunningAsync { get; set; }
        = NamedPipeMcpClient.IsServerRunningAsync;

    /// <summary>
    /// サーバーを起動する関数。
    /// テストでモック可能にするため internal static プロパティとして公開する。
    /// </summary>
    internal static Func<NamedPipeEndPoint, int, CancellationToken, Task<bool>> SpawnServerAsync { get; set; }
        = SpawnServerAsyncImpl;

    /// <summary>
    /// Named Pipe サーバーが起動しているか確認し、未起動の場合は自動起動する。
    /// </summary>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>サーバーが起動していれば/起動できれば true、それ以外は false。</returns>
    public static async Task<bool> EnsureServerRunningAsync(CancellationToken ct = default)
    {
        var endpoint = NamedPipeEndPoint.FromWorkspacePath(NamedPipeEndPoint.ResolveWorkspacePath());

        // パイプの存在確認を先に行う（短いタイムアウトで高速に）
        // パイプが存在しない場合は即座に起動、存在する場合は接続テスト
        var pipeExists = await IsServerRunningAsync(endpoint, 100, ct).ConfigureAwait(false);

        if (pipeExists)
        {
            // パイプが存在する場合は応答確認（既存の動作）
            if (await IsServerRunningAsync(endpoint, 500, ct).ConfigureAwait(false))
            {
                return true;
            }

            // パイプは存在するが応答なしの場合は起動を試みる（古いプロセスの可能性）
        }

        // 未起動または応答なしの場合は spawn（待たずに即座に起動）
        return await SpawnServerAsync(endpoint, DefaultTimeoutMs, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// adact serve pipe を起動し、起動完了を待機する。
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

        Process? process;
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

        // 起動完了メッセージを待機
        var tcs = new TaskCompletionSource<bool>();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data?.Contains("Daemon listening on") == true)
            {
                tcs.TrySetResult(true);
            }
        };

        process.BeginOutputReadLine();

        try
        {
            // 起動完了を待機、またはタイムアウト
            await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs, cts.Token)).ConfigureAwait(false);

            if (tcs.Task.IsCompletedSuccessfully && tcs.Task.Result)
            {
                // 起動完了メッセージを確認したら、接続確認
                await Task.Delay(500, ct).ConfigureAwait(false); // 少し待機して安定化
                return await NamedPipeMcpClient.IsServerRunningAsync(endpoint, 2000, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセルまたはタイムアウト
        }

        // タイムアウトまたは起動失敗
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
        catch
        {
            // 無視
        }

        return false;
    }

    /// <summary>
    /// adact 実行ファイルのパスを取得する。
    /// </summary>
    private static string GetAdactExecutablePath()
    {
        // 現在のプロセスの実行ファイルを使用
        var currentProcess = Process.GetCurrentProcess();
        return currentProcess.MainModule?.FileName ?? "adact";
    }
}
