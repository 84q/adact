using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class ClickCommand
{
    public static Command Build()
    {
        var refArg = new Argument<string>("ref")
        {
            Description = "Element Ref ID like 's1g1e7' (from snapshot).",
        };
        var noSnapshot = new Option<bool>("--no-snapshot") { Description = "Do not capture a snapshot after the action." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var server = new Option<string?>("--server") { Description = "Connection target URL." };

        var cmd = new Command("click", "Click an element identified by an Element Ref.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(server);

        cmd.SetAction(parseResult =>
        {
            _ = parseResult.GetValue(refArg);
            _ = parseResult.GetValue(noSnapshot);
            _ = parseResult.GetValue(snapshotDir);
            _ = parseResult.GetValue(server);
            return CommandHelpers.NotYetImplemented("click");
        });

        return cmd;
    }
}
