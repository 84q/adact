using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class FillCommand
{
    public static Command Build()
    {
        var refArg = new Argument<string>("ref")
        {
            Description = "Element Ref ID like 's1g1e7' (from snapshot).",
        };
        var textArg = new Argument<string>("text")
        {
            Description = "Text value to fill into the element.",
        };
        var noSnapshot = new Option<bool>("--no-snapshot") { Description = "Do not capture a snapshot after the action." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var server = new Option<string?>("--server") { Description = "Connection target URL." };

        var cmd = new Command("fill", "Fill (overwrite) an input element with text.");
        cmd.Arguments.Add(refArg);
        cmd.Arguments.Add(textArg);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(server);

        cmd.SetAction(parseResult =>
        {
            _ = parseResult.GetValue(refArg);
            _ = parseResult.GetValue(textArg);
            _ = parseResult.GetValue(noSnapshot);
            _ = parseResult.GetValue(snapshotDir);
            _ = parseResult.GetValue(server);
            return CommandHelpers.NotYetImplemented("fill");
        });

        return cmd;
    }
}
