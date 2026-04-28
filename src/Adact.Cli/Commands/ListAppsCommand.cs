using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

internal static class ListAppsCommand
{
  public static Command Build()
  {
    var server = CommandHelpers.CreateServerOption();

    var cmd = new Command("list-apps", "List top-level windows on this Windows desktop.");
    cmd.Options.Add(server);

    cmd.SetAction((parseResult, ct) =>
    {
      var serverArg = parseResult.GetValue(server);
      return CommandHelpers.RunWithClientAsync(serverArg, ExecuteAsync, ct);
    });

    return cmd;
  }

  private static async Task<int> ExecuteAsync(AdactMcpClient client, CancellationToken ct)
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
