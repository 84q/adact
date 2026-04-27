using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class SnapshotCommand
{
    public static Command Build()
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
        var noSnapshot = new Option<bool>("--no-snapshot") { Description = "Do not write the snapshot file (return path only is suppressed)." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var server = new Option<string?>("--server") { Description = "Connection target URL." };

        var cmd = new Command("snapshot", "Capture a UIA snapshot of the active or specified session.");
        cmd.Options.Add(sid);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(server);

        cmd.SetAction(parseResult =>
        {
            _ = parseResult.GetValue(sid);
            _ = parseResult.GetValue(noSnapshot);
            _ = parseResult.GetValue(snapshotDir);
            _ = parseResult.GetValue(server);
            return CommandHelpers.NotYetImplemented("snapshot");
        });

        return cmd;
    }
}
