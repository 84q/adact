using System.CommandLine;
using System.Net.Sockets;

using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>daemon-stop</c> コマンド。ローカルの HTTP MCP daemon を graceful に停止する。
/// localhost 以外への適用は LOCAL_ONLY として拒否される (設計 009 §3.4 / §6.3)。
/// </summary>
internal static class DaemonStopCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>daemon-stop サブコマンド。</returns>
    public static Command Build()
    {
        var cmd = new Command("daemon-stop", "Stop a local HTTP MCP daemon gracefully.");

        cmd.SetAction((parseResult, ct) =>
        {
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);
            return RunAsync(serverArg, ct);
        });

        return cmd;
    }

    /// <summary>daemon-stop 本体の実行。接続解決と localhost ガードの後、<c>daemon_stop</c> tool を呼び出す。</summary>
    /// <param name="serverArg"><c>--server</c> の値。null なら <c>.adact/config.json</c> / 既定エンドポイントを試行する。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    private static async Task<int> RunAsync(string? serverArg, CancellationToken ct)
    {
        ServerEndpoint endpoint;
        try
        {
            endpoint = ConnectionResolver.Resolve(serverArg);
        }
        catch (InvalidUrlException ex)
        {
            return ConnectionErrors.ReportResolutionError(ex);
        }
        catch (ConfigParseException ex)
        {
            return ConnectionErrors.ReportResolutionError(ex);
        }

        // 設計 §3.4 / §6.3: daemon-stop はローカル接続専用。host が localhost 以外なら
        // MCP 呼び出しを行わずに LOCAL_ONLY エラーで終了する。
        if (!endpoint.IsLocalhost)
        {
            CliError.Write(
                ErrorCodes.LocalOnly,
                $"daemon-stop requires a localhost target, but '{endpoint.Url.Host}' is not local.",
                "run 'adact daemon-stop' on the same host as 'adact serve', or omit --server to use the default localhost endpoint.");
            return ExitCodes.UserError;
        }

        AdactMcpClient? client = null;
        try
        {
            try
            {
                client = await AdactMcpClient.ConnectAsync(endpoint, loggerFactory: null, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return ConnectionErrors.ReportAndReturnExitCode(ex, endpoint);
            }

            CallToolResult result;
            try
            {
                result = await client.CallToolAsync("daemon_stop", arguments: null, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested && IsConnectionDropException(ex))
            {
                // daemon_stop の応答前に daemon が落ちて HTTP セッションが切断されるケース。
                // 設計 §4.5 / §6.x: 切断は「既に停止した」と見なし success 扱い。
                Console.Out.WriteLine("stopped");
                return ExitCodes.Success;
            }

            var errorExit = McpResponse.TryReportError(result);
            if (errorExit is { } code) return code;

            Console.Out.WriteLine("stopped");
            return ExitCodes.Success;
        }
        finally
        {
            // daemon_stop に成功すると daemon プロセスが直ちに終了し、HTTP セッションが
            // 閉じられている可能性が高い。Dispose 時の接続切断系例外は「既に停止した」
            // と見なして握り潰す。設計 §4.5 / §6.x。
            if (client is not null)
            {
                try
                {
                    await client.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (IsConnectionDropException(ex))
                {
                    // ignore
                }
            }
        }
    }

    /// <summary>
    /// daemon との HTTP セッション切断に伴う例外を判定する。
    /// CancellationToken 系 (OperationCanceledException / TaskCanceledException) はユーザの
    /// Ctrl+C 由来で発生しうるため除外する (設計 009 §6.x、Phase5 #8 m2 指摘)。
    /// </summary>
    /// <param name="ex">判定対象の例外。</param>
    /// <returns>HTTP セッション切断とみなせる例外チェーンなら true。</returns>
    internal static bool IsConnectionDropException(Exception ex)
    {
        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is HttpRequestException
                || cur is SocketException
                || cur is IOException
                || cur is ObjectDisposedException)
            {
                return true;
            }
        }
        return false;
    }
}
