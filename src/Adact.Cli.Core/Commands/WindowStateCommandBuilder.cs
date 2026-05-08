using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class WindowStateCommandBuilder
{
    public static Command Build(string name, string description, string toolName)
    {
        var sid = new Argument<string?>("sid") { Arity = ArgumentArity.ZeroOrOne, Description = "Target session ID (default: active session)." };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();

        var cmd = new Command(name, description);
        cmd.Arguments.Add(sid);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var noSnap = parseResult.GetValue(noSnapshot);
            var dirArg = parseResult.GetValue(snapshotDir);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            var args = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(sidArg)) args["sessionId"] = sidArg;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunSessionOperationAndAutoSnapshotAsync(
                    client, name, toolName, args, sidArg, noSnap, dirArg, token),
                ct);
        });

        return cmd;
    }
}
