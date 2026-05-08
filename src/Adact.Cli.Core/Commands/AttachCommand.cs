using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

using ModelContextProtocol.Protocol;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>attach</c> command. Resolves a Window Ref, attaches to the window, and creates a session.
/// See docs/spec/cli.md for the attach flow.
/// </summary>
internal static class AttachCommand
{
    /// <summary>
    /// Arguments for <c>attach</c>. Used by unit tests and snapshot-related options (<c>--no-snapshot</c>/<c>--snapshot-dir</c>).
    /// </summary>
    /// <param name="Ref">Window Ref (for example, <c>w1</c>).</param>
    internal sealed record AttachArgs(string? Ref);

    /// <summary>Creates the System.CommandLine <see cref="Command"/> for <c>attach</c>.</summary>
    /// <returns>The <c>attach</c> command.</returns>
    public static Command Build()
    {
        var refArg = new Argument<string?>("ref")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "Window Ref ID like 'w1' (from list-windows).",
        };
        var noSnapshot = new Option<bool>("--no-snapshot") { Description = "Do not capture a snapshot on success." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };

        var cmd = new Command("attach", "Attach to a window as a session.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);

        cmd.SetAction((parseResult, ct) =>
        {
            var args = new AttachArgs(Ref: parseResult.GetValue(refArg));

            // Validate the arguments.
            var (errorCode, errorMessage) = ValidateAttachArgs(args);
            if (errorCode is not null)
            {
                CliError.Write(errorCode, errorMessage ?? "invalid arguments.");
                return Task.FromResult(ExitCodes.UserError);
            }

            var arguments = new Dictionary<string, object?> { ["windowRef"] = args.Ref };
            var noSnap = parseResult.GetValue(noSnapshot);
            var dir = parseResult.GetValue(snapshotDir);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => ExecuteAsync(client, arguments, noSnap, dir, token),
                ct);
        });

        return cmd;
    }

    /// <summary>
    /// Validates <c>attach</c> arguments (MCP-facing). Returns <c>(errorCode, errorMessage)</c> on failure or <c>(null, null)</c> on success.
    /// </summary>
    /// <param name="args">Arguments for <c>attach</c>.</param>
    /// <returns>A tuple of (error code, error message). Returns null values on success.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is null.</exception>
    internal static (string? errorCode, string? errorMessage) ValidateAttachArgs(AttachArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (string.IsNullOrEmpty(args.Ref))
        {
            return (ErrorCodes.InvalidArgument,
                "Specify a positional ref (w<n>) obtained from list-windows.");
        }

        if (!RefValidator.IsWindowRef(args.Ref))
        {
            return (ErrorCodes.InvalidArgument,
                $"ref must be in 'w<n>' form, got '{args.Ref}'.");
        }

        return (null, null);
    }

    /// <summary>Calls <c>adact_attach</c>, then writes the session/window result and optionally captures a snapshot.</summary>
    /// <param name="client">Connected MCP client.</param>
    /// <param name="arguments">Arguments for <c>adact_attach</c>.</param>
    /// <param name="noSnapshot">When true, skip snapshot capture after attach.</param>
    /// <param name="snapshotDir">Snapshot output directory (defaults to <c>.adact/</c> when null).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Exit code.</returns>
    private static async Task<int> ExecuteAsync(
        IAdactMcpClient client,
        Dictionary<string, object?> arguments,
        bool noSnapshot,
        string? snapshotDir,
        CancellationToken ct)
    {
        var attachResult = await client.CallToolAsync("adact_attach", arguments, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(attachResult);
        if (errorExit is { } code) return code;

        var info = McpResponse.GetJson(attachResult);
        var sessionId = JsonHelpers.GetStringOrNull(info, "sessionId");
        var windowInfo = info.TryGetProperty("windowInfo", out var wi) ? wi : default;
        var processId = JsonHelpers.GetIntAsStringOrNull(windowInfo, "processId");
        var title = JsonHelpers.GetStringOrNull(windowInfo, "windowTitle");

        if (string.IsNullOrEmpty(sessionId))
        {
            CliError.Write(ErrorCodes.InternalError, "adact_attach response missing 'sessionId'.");
            return ExitCodes.CommandFailed;
        }

        var bodyFields = new List<KeyValuePair<string, string?>>
        {
            CliOutput.Field("sessionId", sessionId),
            CliOutput.Field("processId", processId),
            CliOutput.Field("title", title),
        };

        if (noSnapshot)
        {
            CliOutput.WriteYamlSuccess(metaFields: null, bodyFields);
            return ExitCodes.Success;
        }

        return await WriteAttachWithSnapshotAsync(client, sessionId, snapshotDir, bodyFields, ct).ConfigureAwait(false);
    }

    private static async Task<int> WriteAttachWithSnapshotAsync(
        IAdactMcpClient client,
        string sessionId,
        string? snapshotDir,
        IReadOnlyList<KeyValuePair<string, string?>> bodyFields,
        CancellationToken ct)
    {
        var snapshotResult = await client.CallToolAsync(
            "adact_snapshot",
            new Dictionary<string, object?> { ["sessionId"] = sessionId },
            ct).ConfigureAwait(false);
        var errorExit = McpResponse.TryReportError(snapshotResult);
        if (errorExit is { } code) return code;

        var snapJson = McpResponse.GetJson(snapshotResult);
        var raw = snapshotResult.Content is { Count: > 0 } content && content[0] is TextContentBlock tcb && !string.IsNullOrEmpty(tcb.Text)
            ? tcb.Text
            : snapJson.GetRawText();

        string text;
        try
        {
            var (meta, root) = Snapshots.SnapshotJsonParser.Parse(raw);
            var filtered = Snapshots.SnapshotTreeFilter.Apply(root, Snapshots.SnapshotTreeFilter.FilterOperable);
            text = Snapshots.SnapshotTextFormatter.Format(meta, filtered, Snapshots.SnapshotTreeFilter.FilterOperable);
        }
        catch (JsonException ex)
        {
            CliError.Write(ErrorCodes.InternalError, $"Failed to parse snapshot response: {ex.Message}");
            return ExitCodes.CommandFailed;
        }

        var (path, isNew) = Snapshots.SnapshotFileWriter.Write(text, int.Parse(sessionId[1..], System.Globalization.CultureInfo.InvariantCulture), snapshotDir);
        var snapshotPath = $"{path} {(isNew ? "(changed)" : "(unchanged)")}";
        CliOutput.WriteYamlSuccess([CliOutput.Field("snapshotPath", snapshotPath)], bodyFields);
        return ExitCodes.Success;
    }
}
