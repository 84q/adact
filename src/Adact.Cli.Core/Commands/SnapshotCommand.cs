using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class SnapshotCommand
{
    public static Command Build()
    {
        var sid = new Argument<string?>("sid")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Target session ID (default: active session).",
        };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var filter = new Option<string?>("--filter") { Description = "Tree filter: 'operable' (default) or 'raw'." };

        var cmd = new Command("snapshot", "Capture a UIA snapshot of the active or specified session.");
        cmd.Arguments.Add(sid);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(filter);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var dirArg = parseResult.GetValue(snapshotDir);
            var filterArg = parseResult.GetValue(filter);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.WriteSnapshotResultAsync(
                    client, sidArg, dirArg, token, writeSessionId: true, filter: filterArg, writeContentToStdout: true),
                ct);
        });

        return cmd;
    }
}
