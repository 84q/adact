using System.CommandLine;
using System.IO.Pipes;

using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>daemon-stop</c> コマンド。Named Pipe 接続の MCP daemon を graceful に停止する。
/// HTTP モードへの適用は LOCAL_ONLY として拒否される。
/// </summary>
internal static class DaemonStopCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>daemon-stop サブコマンド。</returns>
    public static Command Build()
    {
        var cmd = new Command("daemon-stop", "Stop a local Named Pipe MCP daemon gracefully.");

        cmd.SetAction((parseResult, ct) =>
        {
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);
            return RunAsync(serverArg, ct);
        });

        return cmd;
    }

    /// <summary>daemon-stop 本体の実行。--server 指定時は HTTP 不可エラー、未指定時は Named Pipe で停止。</summary>
    /// <param name="serverArg"><c>--server</c> の値。null/空白なら Named Pipe 接続を試行する。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    private static async Task<int> RunAsync(string? serverArg, CancellationToken ct)
    {
        // --server 指定時は HTTP モードへの停止を拒否
        if (!string.IsNullOrWhiteSpace(serverArg))
        {
            CliError.Write(
                ErrorCodes.LocalOnly,
                "daemon-stop is not supported for HTTP mode. Use Ctrl+C to stop the server.",
                "For HTTP server, stop the process manually or use task management tools.");
            return ExitCodes.UserError;
        }

        // Named Pipe エンドポイントを解決
        var endpoint = ConnectionResolver.ResolveNamedPipeEndpoint();

        await using var client = await ConnectNamedPipeAsync(endpoint, ct).ConfigureAwait(false);
        if (client is null)
        {
            CliOutput.WriteYamlSuccess(metaFields: null, [CliOutput.Field("stopped", "false"), CliOutput.Field("message", "No daemon is running")]);
            return ExitCodes.Success;
        }

        CallToolResult result;
        try
        {
            result = await client.CallToolAsync("daemon_stop", arguments: null, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested && IsConnectionDropException(ex))
        {
            // daemon_stop の応答前に daemon が落ちてセッションが切断されるケース。
            // 切断は「既に停止した」と見なし success 扱い。
            CliOutput.WriteYamlSuccess(metaFields: null, [CliOutput.Field("stopped", "true")]);
            return ExitCodes.Success;
        }

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        // サーバーが実際に停止したか確認
        await Task.Delay(500, ct).ConfigureAwait(false);

        var isRunning = await NamedPipeMcpClient.IsServerRunningAsync(endpoint, 1000, ct).ConfigureAwait(false);
        if (isRunning)
        {
            CliError.Write(
                ErrorCodes.InternalError,
                "Server did not stop after daemon_stop command.",
                "The daemon_stop command was sent but the server is still running.");
            return ExitCodes.CommandFailed;
        }

        CliOutput.WriteYamlSuccess(metaFields: null, [CliOutput.Field("stopped", "true")]);
        return ExitCodes.Success;
    }

    /// <summary>Named Pipe に接続する。接続失敗時は null を返す。</summary>
    private static async Task<NamedPipeMcpClient?> ConnectNamedPipeAsync(NamedPipeEndPoint endpoint, CancellationToken ct)
    {
        // 先に短時間でパイプの存在確認（100ms）
        var isRunning = await NamedPipeMcpClient.IsServerRunningAsync(endpoint, 100, ct).ConfigureAwait(false);
        if (!isRunning)
        {
            return null; // パイプなし = すぐに返す
        }

        // パイプがある場合は本接続
        try
        {
            return await NamedPipeMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false);
        }
        catch
        {
            // 接続失敗は呼び出し元で「デーモン未起動」として処理する
            return null;
        }
    }

    /// <summary>
    /// daemon とのセッション切断に伴う例外を判定する。
    /// CancellationToken 系 (OperationCanceledException / TaskCanceledException) はユーザの
    /// Ctrl+C 由来で発生しうるため除外する。
    /// </summary>
    /// <param name="ex">判定対象の例外。</param>
    /// <returns>セッション切断とみなせる例外チェーンなら true。</returns>
    internal static bool IsConnectionDropException(Exception ex)
    {
        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is IOException
                || cur is ObjectDisposedException)
            {
                return true;
            }
        }
        return false;
    }
}
