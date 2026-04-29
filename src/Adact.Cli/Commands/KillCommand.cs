using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class KillCommand
{
    public static Command Build()
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("kill", "Terminate the process backing a session (auto-detach on success).");
        cmd.Options.Add(sid);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var serverArg = parseResult.GetValue(server);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => LifecycleCommandImpl.ExecuteAsync(
                    client, "windows_kill", sidArg, ["killed", "detached"], token),
                ct);
        });

        return cmd;
    }
}
