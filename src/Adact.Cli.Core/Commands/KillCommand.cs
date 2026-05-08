using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class KillCommand
{
    public static Command Build()
    {
        var sid = new Argument<string?>("sid") { Arity = ArgumentArity.ZeroOrOne, Description = "Target session ID (default: active session)." };
        var forceOption = new Option<bool>("--force") { Description = "Skip WM_CLOSE and immediately kill the process." };
        var timeoutOption = new Option<int?>("--timeout") { Description = "Graceful shutdown timeout in milliseconds. Defaults to 5000." };

        var cmd = new Command("kill", "Terminate the process backing a session (auto-detach on success).");
        cmd.Arguments.Add(sid);
        cmd.Options.Add(forceOption);
        cmd.Options.Add(timeoutOption);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);
            var force = parseResult.GetValue(forceOption);
            var timeout = parseResult.GetValue(timeoutOption);

            var extraArgs = new Dictionary<string, object?>();
            if (force)
                extraArgs["force"] = true;
            if (timeout is not null)
                extraArgs["timeoutMs"] = timeout.Value;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => LifecycleCommandImpl.ExecuteAsync(
                    client, "adact_kill", sidArg,
                    extraArgs.Count > 0 ? extraArgs : null,
                    ["killed", "detached"],
                    ["method"],
                    token),
                ct);
        });

        return cmd;
    }
}
