using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>close-window</c> コマンド。UIA <c>WindowPattern.Close</c> を呼び、成功時は session も detach される。
/// </summary>
internal static class CloseWindowCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>close-window サブコマンド。</returns>
    public static Command Build()
    {
        var sid = new Argument<string?>("sid") { Arity = ArgumentArity.ZeroOrOne, Description = "Target session ID (default: active session)." };

        var cmd = new Command("close-window", "Close a window via UIA WindowPattern.Close (auto-detach on success).");
        cmd.Arguments.Add(sid);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => LifecycleCommandImpl.ExecuteAsync(
                    client, "adact_close_window", sidArg, ["closed", "detached"], token),
                ct);
        });

        return cmd;
    }
}
