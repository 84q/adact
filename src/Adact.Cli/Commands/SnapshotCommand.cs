using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class SnapshotCommand
{
    public static Command Build()
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var filter = new Option<string?>("--filter") { Description = "Tree filter: 'operable' (default) or 'raw'." };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("snapshot", "Capture a UIA snapshot of the active or specified session.");
        cmd.Options.Add(sid);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(filter);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var dirArg = parseResult.GetValue(snapshotDir);
            var filterArg = parseResult.GetValue(filter);
            var serverArg = parseResult.GetValue(server);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.WriteSnapshotResultAsync(
                    client, sidArg, dirArg, token, writeSessionId: true, filter: filterArg),
                ct);
        });

        return cmd;
    }
}
