using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class CloseAllCommand
{
    public static Command Build()
    {
        var server = new Option<string?>("--server") { Description = "Connection target URL." };

        var cmd = new Command("close-all", "Close all attached windows (per-session result on stdout).");
        cmd.Options.Add(server);

        cmd.SetAction(parseResult =>
        {
            _ = parseResult.GetValue(server);
            return CommandHelpers.NotYetImplemented("close-all");
        });

        return cmd;
    }
}
