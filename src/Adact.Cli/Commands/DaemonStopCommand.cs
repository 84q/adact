using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class DaemonStopCommand
{
    public static Command Build()
    {
        var server = new Option<string?>("--server") { Description = "Connection target URL (must be localhost)." };

        var cmd = new Command("daemon-stop", "Stop a local HTTP MCP daemon gracefully.");
        cmd.Options.Add(server);

        cmd.SetAction(parseResult =>
        {
            _ = parseResult.GetValue(server);
            return CommandHelpers.NotYetImplemented("daemon-stop");
        });

        return cmd;
    }
}
