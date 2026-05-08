using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

internal static class KeypressCommand
{
    public static Command Build()
    {
        var keyArg = new Argument<string>("key")
        {
            Description = "Key combo such as 'Enter', 'F5', or 'Ctrl+Shift+E'.",
        };
        var cmd = new Command("keypress", "Press a key combo (e.g. 'Ctrl+C').");
        cmd.Arguments.Add(keyArg);

        cmd.SetAction((pr, ct) =>
        {
            var key = pr.GetValue(keyArg);
            var serverArg = pr.GetValue(CommandHelpers.ServerOption);
            if (string.IsNullOrEmpty(key))
                return Task.FromResult(OperationOptions.ReportUserError("key argument is required."));

            var args = new Dictionary<string, object?> { ["key"] = key };

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                async (client, token) =>
                {
                    var r = await client.CallToolAsync("adact_keypress", args, token).ConfigureAwait(false);
                    var err = McpResponse.TryReportError(r);
                    if (err is { } code) return code;
                    CliOutput.WriteEmptySuccess();
                    return ExitCodes.Success;
                },
                ct);
        });
        return cmd;
    }
}
