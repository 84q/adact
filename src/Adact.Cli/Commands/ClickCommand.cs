using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

internal static class ClickCommand
{
    public static Command Build()
    {
        var refArg = new Argument<string>("ref")
        {
            Description = "Element Ref ID like 's1e7' (from snapshot).",
        };
        var noSnapshot = new Option<bool>("--no-snapshot") { Description = "Do not capture a snapshot after the action." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("click", "Click an element identified by an Element Ref.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var refValue = parseResult.GetValue(refArg);
            var noSnap = parseResult.GetValue(noSnapshot);
            var dirArg = parseResult.GetValue(snapshotDir);
            var serverArg = parseResult.GetValue(server);

            if (!RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }

            var args = new Dictionary<string, object?> { ["ref"] = refValue };
            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                    client, "windows_click", args, refValue!, noSnap, dirArg, token),
                ct);
        });

        return cmd;
    }
}
