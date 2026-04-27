using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class KillCommand
{
    public static Command Build()
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
        var server = new Option<string?>("--server") { Description = "Connection target URL." };

        var cmd = new Command("kill", "Terminate the process backing a session (auto-detach on success).");
        cmd.Options.Add(sid);
        cmd.Options.Add(server);

        cmd.SetAction(parseResult =>
        {
            _ = parseResult.GetValue(sid);
            _ = parseResult.GetValue(server);
            return CommandHelpers.NotYetImplemented("kill");
        });

        return cmd;
    }
}
