using System.CommandLine;
using System.Globalization;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;
using Adact.Cli.Snapshots;

using ModelContextProtocol.Protocol;

namespace Adact.Cli.Commands;

internal static class SnapshotCommand
{
    public static Command Build()
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("snapshot", "Capture a UIA snapshot of the active or specified session.");
        cmd.Options.Add(sid);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var dirArg = parseResult.GetValue(snapshotDir);
            var serverArg = parseResult.GetValue(server);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => ExecuteAsync(client, sidArg, dirArg, token),
                ct);
        });

        return cmd;
    }

    private static async Task<int> ExecuteAsync(
        AdactMcpClient client,
        string? sessionId,
        string? snapshotDir,
        CancellationToken ct)
    {
        IReadOnlyDictionary<string, object?>? args = sessionId is null
            ? null
            : new Dictionary<string, object?> { ["sessionId"] = sessionId };

        var result = await client.CallToolAsync("windows_snapshot", args, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var json = McpResponse.GetJson(result);
        var meta = json.ValueKind == JsonValueKind.Object && json.TryGetProperty("_meta", out var m)
            ? m
            : default;

        var resolvedSid = (meta.ValueKind == JsonValueKind.Object
            ? JsonHelpers.GetStringOrNull(meta, "sessionId")
            : null) ?? sessionId;
        var generation = meta.ValueKind == JsonValueKind.Object
            ? JsonHelpers.GetIntOrNull(meta, "generation")
            : null;

        if (string.IsNullOrEmpty(resolvedSid))
        {
            CliError.Write(ErrorCodes.InternalError, "windows_snapshot response missing sessionId.");
            return ExitCodes.CommandFailed;
        }

        var raw = ExtractSnapshotJsonText(result, json);
        var sidNum = ParseSidNumber(resolvedSid);
        var path = SnapshotFileWriter.Write(raw, sidNum, generation ?? 0, snapshotDir);

        KeyValueWriter.Write("sessionId", resolvedSid);
        if (generation is { } g)
        {
            KeyValueWriter.Write("generation", g.ToString(CultureInfo.InvariantCulture));
        }
        KeyValueWriter.Write("snapshot", path);
        return ExitCodes.Success;
    }

    private static int ParseSidNumber(string sessionId)
    {
        if (sessionId.Length >= 2 && sessionId[0] == 's'
            && int.TryParse(sessionId.AsSpan(1), out var n))
        {
            return n;
        }
        return 0;
    }

    private static string ExtractSnapshotJsonText(CallToolResult result, JsonElement parsed)
    {
        if (result.Content is { Count: > 0 } content
            && content[0] is TextContentBlock tcb
            && !string.IsNullOrEmpty(tcb.Text))
        {
            return tcb.Text;
        }
        return parsed.GetRawText();
    }
}
