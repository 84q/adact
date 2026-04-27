using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class DetachCommand
{
    public static Command Build()
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
        var server = new Option<string?>("--server") { Description = "Connection target URL." };

        var cmd = new Command("detach", "Release a session (window stays intact).");
        cmd.Options.Add(sid);
        cmd.Options.Add(server);

        cmd.SetAction(parseResult =>
        {
            _ = parseResult.GetValue(sid);
            _ = parseResult.GetValue(server);
            return CommandHelpers.NotYetImplemented("detach");
        });

        return cmd;
    }
}
