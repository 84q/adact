using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

internal static class KeyupCommand
{
    public static Command Build()
    {
        var keyArg = new Argument<string>("key")
        {
            Description = "Single key name (must match the one passed to 'keydown').",
        };

        var cmd = new Command("keyup", "Release a single key previously pressed by 'keydown'.");
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
                    var r = await client.CallToolAsync("adact_keyup", args, token).ConfigureAwait(false);
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
