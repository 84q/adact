using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>snapshot</c> コマンド。指定 (もしくは active) session に対して UIA snapshot を取得し、
/// CLI 側でフィルタとテキスト整形を適用して <c>.adact/</c> に保存する。
/// </summary>
internal static class SnapshotCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>snapshot サブコマンド。</returns>
    public static Command Build()
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var filter = new Option<string?>("--filter") { Description = "Tree filter: 'operable' (default) or 'raw'." };

        var cmd = new Command("snapshot", "Capture a UIA snapshot of the active or specified session.");
        cmd.Options.Add(sid);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(filter);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var dirArg = parseResult.GetValue(snapshotDir);
            var filterArg = parseResult.GetValue(filter);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.WriteSnapshotResultAsync(
                    client, sidArg, dirArg, token, writeSessionId: true, filter: filterArg, writeContentToStdout: true),
                ct);
        });

        return cmd;
    }
}
