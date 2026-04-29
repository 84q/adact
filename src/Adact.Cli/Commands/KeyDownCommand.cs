using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>key-down</c> コマンド。単一キーを押下保持する (低レベル)。</summary>
internal static class KeyDownCommand
{
    /// <summary>key-down サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var keyArg = new Argument<string>("key")
        {
            Description = "Single key name (e.g. 'Shift', 'A', 'F1'). '+' combinations are not allowed.",
        };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("key-down", "Press and hold a single key on the active session's window.");
        cmd.Arguments.Add(keyArg);
        cmd.Options.Add(server);

        cmd.SetAction((pr, ct) =>
        {
            var key = pr.GetValue(keyArg);
            var serverArg = pr.GetValue(server);
            if (string.IsNullOrEmpty(key))
                return Task.FromResult(OperationOptions.ReportUserError("key argument is required."));

            var args = new Dictionary<string, object?> { ["key"] = key };
            return CommandHelpers.RunWithClientAsync(
                serverArg,
                async (client, token) =>
                {
                    var r = await client.CallToolAsync("windows_key_down", args, token).ConfigureAwait(false);
                    return McpResponse.TryReportError(r) ?? ExitCodes.Success;
                },
                ct);
        });
        return cmd;
    }
}
