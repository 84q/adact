using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>close</c> コマンド。UIA <c>WindowPattern.Close</c> を呼び、成功時は session も detach される。
/// </summary>
internal static class CloseCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>close サブコマンド。</returns>
    public static Command Build()
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("close", "Close a window via UIA WindowPattern.Close (auto-detach on success).");
        cmd.Options.Add(sid);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var serverArg = parseResult.GetValue(server);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => LifecycleCommandImpl.ExecuteAsync(
                    client, "windows_close", sidArg, ["closed", "detached"], token),
                ct);
        });

        return cmd;
    }
}
