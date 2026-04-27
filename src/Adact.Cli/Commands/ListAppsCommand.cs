using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class ListAppsCommand
{
    public static Command Build()
    {
        var server = new Option<string?>("--server")
        {
            Description = "Connection target URL (overrides .adact/config.json).",
        };

        var cmd = new Command("list-apps", "List top-level windows on this Windows desktop.");
        cmd.Options.Add(server);

        cmd.SetAction(parseResult =>
        {
            _ = parseResult.GetValue(server);
            return CommandHelpers.NotYetImplemented("list-apps");
        });

        return cmd;
    }
}
