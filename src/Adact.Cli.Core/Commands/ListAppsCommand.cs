using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>list-apps</c> コマンド。<c>windows_list_apps</c> tool を呼び、一覧を TSV で stdout に出力する。
/// </summary>
internal static class ListAppsCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>list-apps サブコマンド。</returns>
    public static Command Build()
    {
        var cmd = new Command("list-apps", "List top-level windows on this Windows desktop.");

        cmd.SetAction((parseResult, ct) =>
        {
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);
            // list-apps は自動起動対象
            return CommandHelpers.RunWithClientAndAutoStartAsync(serverArg, ExecuteAsync, ct);
        });

        return cmd;
    }

    /// <summary><c>windows_list_apps</c> を呼び TSV として stdout に出力する。</summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    private static async Task<int> ExecuteAsync(IAdactMcpClient client, CancellationToken ct)
    {
        var result = await client.CallToolAsync("windows_list_apps", arguments: null, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var json = McpResponse.GetJson(result);

        // StructuredContent は { "windows": [...] }、Content[0].Text は raw 配列。両対応。
        JsonElement windows;
        if (json.ValueKind == JsonValueKind.Array)
        {
            windows = json;
        }
        else if (json.ValueKind == JsonValueKind.Object
                 && json.TryGetProperty("windows", out var w)
                 && w.ValueKind == JsonValueKind.Array)
        {
            windows = w;
        }
        else
        {
            CliError.Write(ErrorCodes.InternalError, "windows_list_apps response missing 'windows' array.");
            return ExitCodes.CommandFailed;
        }

        TsvWriter.WriteHeader("windowRef", "sessionId", "processName", "processId", "className", "windowTitle");
        foreach (var entry in windows.EnumerateArray())
        {
            TsvWriter.WriteRow(
                JsonHelpers.GetStringOrNull(entry, "windowRef"),
                JsonHelpers.GetStringOrNull(entry, "sessionId"),
                JsonHelpers.GetStringOrNull(entry, "processName"),
                JsonHelpers.GetIntAsStringOrNull(entry, "processId"),
                JsonHelpers.GetStringOrNull(entry, "className"),
                JsonHelpers.GetStringOrNull(entry, "windowTitle"));
        }

        return ExitCodes.Success;
    }
}
