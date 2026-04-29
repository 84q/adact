using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class DetachCommand
{
    public static Command Build()
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("detach", "Release a session (window stays intact).");
        cmd.Options.Add(sid);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var serverArg = parseResult.GetValue(server);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => LifecycleCommandImpl.ExecuteAsync(
                    client, "windows_detach", sidArg, ["detached"], token),
                ct);
        });

        return cmd;
    }
}
