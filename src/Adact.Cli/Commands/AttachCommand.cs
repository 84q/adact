using System.CommandLine;
using System.Text.Json;
using System.Text.RegularExpressions;

using Adact.Cli.Connection;
using Adact.Cli.Output;
using Adact.Cli.Snapshots;

namespace Adact.Cli.Commands;

internal static class AttachCommand
{
    private static readonly Regex WindowRefPattern = new("^w\\d+$", RegexOptions.Compiled);

    public static Command Build()
    {
        var refArg = new Argument<string?>("ref")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Window Ref ID like 'w1' (from list-apps).",
        };
        var processName = new Option<string?>("--process-name") { Description = "Process name (e.g. CalculatorApp)." };
        var title = new Option<string?>("--title") { Description = "Window title (case-insensitive, exact match)." };
        var processId = new Option<int?>("--process-id") { Description = "Process ID." };
        var className = new Option<string?>("--class-name") { Description = "Win32 class name." };
        var noSnapshot = new Option<bool>("--no-snapshot") { Description = "Do not capture a snapshot on success." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("attach", "Attach to a window as a session.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(processName);
        cmd.Options.Add(title);
        cmd.Options.Add(processId);
        cmd.Options.Add(className);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var args = new AttachArgs(
                Ref: parseResult.GetValue(refArg),
                ProcessName: parseResult.GetValue(processName),
                Title: parseResult.GetValue(title),
                ProcessId: parseResult.GetValue(processId),
                ClassName: parseResult.GetValue(className),
                NoSnapshot: parseResult.GetValue(noSnapshot),
                SnapshotDir: parseResult.GetValue(snapshotDir));

            // 引数バリデーションは接続前に実施する。
            var validationExit = ValidateArgs(args, out var arguments);
            if (validationExit is { } code) return Task.FromResult(code);

            var serverArg = parseResult.GetValue(server);
            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => ExecuteAsync(client, arguments!, args, token),
                ct);
        });

        return cmd;
    }

    private sealed record AttachArgs(
        string? Ref,
        string? ProcessName,
        string? Title,
        int? ProcessId,
        string? ClassName,
        bool NoSnapshot,
        string? SnapshotDir);

    private static int? ValidateArgs(AttachArgs args, out Dictionary<string, object?>? arguments)
    {
        arguments = null;
        var hasFlags = args.ProcessName is not null
            || args.Title is not null
            || args.ProcessId is not null
            || args.ClassName is not null;

        if (!string.IsNullOrEmpty(args.Ref))
        {
            if (!WindowRefPattern.IsMatch(args.Ref))
            {
                CliError.Write(ErrorCodes.InvalidArgument,
                    $"ref must be in 'w<n>' form, got '{args.Ref}'.");
                return ExitCodes.UserError;
            }
            if (hasFlags)
            {
                CliError.Write(ErrorCodes.InvalidArgument,
                    "Positional ref and matching flags (--process-name/--title/--process-id/--class-name) are mutually exclusive.");
                return ExitCodes.UserError;
            }
            arguments = new Dictionary<string, object?> { ["windowRef"] = args.Ref };
            return null;
        }

        if (!hasFlags)
        {
            CliError.Write(ErrorCodes.InvalidArgument,
                "Specify either positional ref (w<n>) or at least one of --process-name/--title/--process-id/--class-name.");
            return ExitCodes.UserError;
        }

        var dict = new Dictionary<string, object?>();
        if (args.ProcessName is not null) dict["processName"] = args.ProcessName;
        if (args.Title is not null) dict["windowTitle"] = args.Title;
        if (args.ClassName is not null) dict["className"] = args.ClassName;
        if (args.ProcessId is not null) dict["processId"] = args.ProcessId.Value;
        arguments = dict;
        return null;
    }

    private static async Task<int> ExecuteAsync(
        AdactMcpClient client,
        Dictionary<string, object?> arguments,
        AttachArgs args,
        CancellationToken ct)
    {
        var attachResult = await client.CallToolAsync("windows_attach", arguments, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(attachResult);
        if (errorExit is { } code) return code;

        var info = McpResponse.GetJson(attachResult);
        var sessionId = JsonHelpers.GetStringOrNull(info, "sessionId");
        var windowRef = JsonHelpers.GetStringOrNull(info, "windowRef");

        if (string.IsNullOrEmpty(sessionId))
        {
            CliError.Write(ErrorCodes.InternalError, "windows_attach response missing 'sessionId'.");
            return ExitCodes.CommandFailed;
        }

        // snapshot 取得 (--no-snapshot でなければ)
        int? generation = null;
        string? snapshotPath = null;
        if (!args.NoSnapshot)
        {
            var snapResult = await client.CallToolAsync(
                "windows_snapshot",
                new Dictionary<string, object?> { ["sessionId"] = sessionId },
                ct).ConfigureAwait(false);

            var snapErrorExit = McpResponse.TryReportError(snapResult);
            if (snapErrorExit is { } snapCode) return snapCode;

            var snapJson = McpResponse.GetJson(snapResult);
            generation = ExtractGeneration(snapJson);
            var sid = ParseSidNumber(sessionId);
            var raw = SnapshotJsonText(snapResult, snapJson);
            snapshotPath = SnapshotFileWriter.Write(raw, sid, generation ?? 0, args.SnapshotDir);
        }

        KeyValueWriter.Write("sessionId", sessionId);
        if (!string.IsNullOrEmpty(windowRef)) KeyValueWriter.Write("windowRef", windowRef);
        if (generation is { } g) KeyValueWriter.Write("generation", g.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(snapshotPath)) KeyValueWriter.Write("snapshot", snapshotPath);

        return ExitCodes.Success;
    }

    private static int ParseSidNumber(string sessionId)
    {
        // "s1" → 1
        if (sessionId.Length >= 2 && sessionId[0] == 's'
            && int.TryParse(sessionId.AsSpan(1), out var n))
        {
            return n;
        }
        return 0;
    }

    private static int? ExtractGeneration(JsonElement snapshotJson)
    {
        if (snapshotJson.ValueKind != JsonValueKind.Object) return null;
        if (!snapshotJson.TryGetProperty("_meta", out var meta)) return null;
        return JsonHelpers.GetIntOrNull(meta, "generation");
    }

    private static string SnapshotJsonText(
        ModelContextProtocol.Protocol.CallToolResult result,
        JsonElement parsed)
    {
        // 元の JSON 文字列をそのまま保存する (Content[0].Text 優先)
        if (result.Content is { Count: > 0 } content
            && content[0] is ModelContextProtocol.Protocol.TextContentBlock tcb
            && !string.IsNullOrEmpty(tcb.Text))
        {
            return tcb.Text;
        }
        return parsed.GetRawText();
    }
}
