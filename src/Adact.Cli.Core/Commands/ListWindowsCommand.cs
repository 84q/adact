using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class ListWindowsCommand
{
    public static Command Build()
    {
        var cmd = new Command("list-windows", "List top-level windows on this Windows desktop.");

        cmd.SetAction((parseResult, ct) =>
        {
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);
            return CommandHelpers.RunWithClientAndAutoStartAsync(serverArg, ExecuteAsync, ct);
        });

        return cmd;
    }

    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    private static async Task<int> ExecuteAsync(IAdactMcpClient client, CancellationToken ct)
    {
        var result = await client.CallToolAsync("adact_list_windows", arguments: null, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var json = McpResponse.GetJson(result);

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
            CliError.Write(ErrorCodes.InternalError, "adact_list_windows response missing 'windows' array.");
            return ExitCodes.CommandFailed;
        }

        var rows = new List<string?[]>();
        foreach (var entry in windows.EnumerateArray())
        {
            rows.Add(
            [
                JsonHelpers.GetStringOrNull(entry, "windowRef"),
                JsonHelpers.GetStringOrNull(entry, "sessionId"),
                JsonHelpers.GetStringOrNull(entry, "processName"),
                JsonHelpers.GetIntAsStringOrNull(entry, "processId"),
                JsonHelpers.GetStringOrNull(entry, "className"),
                JsonHelpers.GetStringOrNull(entry, "windowTitle")]);
        }

        CliOutput.WriteTsvResult(true,
            ["windowRef", "sessionId", "processName", "processId", "className", "windowTitle"],
            rows);

        return ExitCodes.Success;
    }
}
