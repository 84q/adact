using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>detach</c> コマンド。session を解放するが window 本体はそのまま残す。
/// </summary>
internal static class DetachCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>detach サブコマンド。</returns>
    public static Command Build()
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };

        var cmd = new Command("detach", "Release a session (window stays intact).");
        cmd.Options.Add(sid);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => LifecycleCommandImpl.ExecuteAsync(
                    client, "windows_detach", sidArg, ["detached"], token),
                ct);
        });

        return cmd;
    }
}
