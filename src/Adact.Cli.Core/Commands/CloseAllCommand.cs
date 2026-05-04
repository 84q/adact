using System.CommandLine;
using System.Text;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>close-all</c> コマンド。接続中のすべての session を close し、セッションごとの結果を TSV で stdout に出力する。
/// </summary>
internal static class CloseAllCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>close-all サブコマンド。</returns>
    public static Command Build()
    {
        var cmd = new Command("close-all", "Close all attached windows (per-session result on stdout).");

        cmd.SetAction((parseResult, ct) =>
        {
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => ExecuteAsync(client, token),
                ct);
        });

        return cmd;
    }

    /// <summary><c>windows_close_all</c> を呼び出し、結果を TSV として stdout に書き出して exit code を決定する。</summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。全 ok もしくは空配列なら 0、一つでも fail があれば 1。</returns>
    private static async Task<int> ExecuteAsync(IAdactMcpClient client, CancellationToken ct)
    {
        var result = await client.CallToolAsync("windows_close_all", arguments: null, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var info = McpResponse.GetJson(result);
        var (rows, exit, errorMessage) = FormatResults(info);
        if (errorMessage is not null)
        {
            CliError.Write(ErrorCodes.InternalError, errorMessage);
            return ExitCodes.CommandFailed;
        }

        CliOutput.WriteTsvResult(exit == ExitCodes.Success, ["sessionId", "result", "error"], rows);
        return exit;
    }

    /// <summary>
    /// <c>windows_close_all</c> レスポンスの <c>results</c> 配列を TSV 文字列 (ヘッダ無し) に変換する。
    /// 設計 §4.5 / §5.2。1 つでも fail があれば exit 1、すべて ok なら exit 0、空配列も exit 0。
    /// </summary>
    /// <param name="info"><c>windows_close_all</c> のレスポンス JSON オブジェクト。</param>
    /// <returns>(stdout に書き出すべき TSV 行, exit code, malformed 時のエラーメッセージ)</returns>
    internal static (IReadOnlyList<string?[]> rows, int exit, string? errorMessage) FormatResults(JsonElement info)
    {
        var rows = new List<string?[]>();
        var hasFailure = false;

        if (info.ValueKind != JsonValueKind.Object
            || !info.TryGetProperty("results", out var results)
            || results.ValueKind != JsonValueKind.Array)
        {
            return (rows, ExitCodes.CommandFailed,
                "windows_close_all response missing 'results' array.");
        }

        foreach (var entry in results.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                return (rows, ExitCodes.CommandFailed,
                    "windows_close_all response contains a non-object entry in 'results'.");
            }

            var sid = JsonHelpers.GetStringOrNull(entry, "sessionId") ?? "";
            var resultStr = JsonHelpers.GetStringOrNull(entry, "result") ?? "";
            var error = JsonHelpers.GetStringOrNull(entry, "error");

            var outputResult = string.Equals(resultStr, "ok", StringComparison.Ordinal) ? "true" : "false";
            string? outputError = null;
            if (!string.Equals(resultStr, "ok", StringComparison.Ordinal))
            {
                hasFailure = true;
                if (!string.IsNullOrEmpty(error))
                {
                    outputError = error;
                }
            }
            rows.Add([sid, outputResult, outputError]);
        }

        var exit = hasFailure ? ExitCodes.CommandFailed : ExitCodes.Success;
        return (rows, exit, null);
    }
}
