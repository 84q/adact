using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// minimize / maximize / restore の共通ビルダ。--sid / --no-snapshot / --snapshot-dir / --server のみを持ち、
/// 引数は不要なシンプル形式。auto-snapshot あり。
/// </summary>
internal static class WindowStateCommandBuilder
{
    /// <summary>引数無しでアタッチ済みウィンドウへ操作する系コマンドを構築する。</summary>
    /// <param name="name">CLI サブコマンド名 (kebab-case)。</param>
    /// <param name="description">--help に表示する説明文。</param>
    /// <param name="toolName">対応する MCP ツール名 (例: <c>windows_minimize</c>)。</param>
    /// <returns>System.CommandLine の <see cref="Command"/>。</returns>
    public static Command Build(string name, string description, string toolName)
    {
        var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command(name, description);
        cmd.Options.Add(sid);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var noSnap = parseResult.GetValue(noSnapshot);
            var dirArg = parseResult.GetValue(snapshotDir);
            var serverArg = parseResult.GetValue(server);

            var args = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(sidArg)) args["sessionId"] = sidArg;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunSessionOperationAndAutoSnapshotAsync(
                    client, toolName, args, sidArg, noSnap, dirArg, token),
                ct);
        });

        return cmd;
    }
}
