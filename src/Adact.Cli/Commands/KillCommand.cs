using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>kill</c> コマンド。session に紐づくプロセスを Process.Kill で強制終了し、session も detach される。
/// </summary>
internal static class KillCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>kill サブコマンド。</returns>
    public static Command Build()
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("kill", "Terminate the process backing a session (auto-detach on success).");
        cmd.Options.Add(sid);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var serverArg = parseResult.GetValue(server);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => LifecycleCommandImpl.ExecuteAsync(
                    client, "windows_kill", sidArg, ["killed", "detached"], token),
                ct);
        });

        return cmd;
    }
}
