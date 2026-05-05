using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// minimize / maximize / restore の共通ビルダ。sid(位置引数, 任意) / --no-snapshot / --snapshot-dir / --server を持つ。
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
        var sid = new Argument<string?>("sid") { Arity = ArgumentArity.ZeroOrOne, Description = "Target session ID (default: active session)." };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();

        var cmd = new Command(name, description);
        cmd.Arguments.Add(sid);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);

        cmd.SetAction((parseResult, ct) =>
        {
            var sidArg = parseResult.GetValue(sid);
            var noSnap = parseResult.GetValue(noSnapshot);
            var dirArg = parseResult.GetValue(snapshotDir);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            var args = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(sidArg)) args["sessionId"] = sidArg;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunSessionOperationAndAutoSnapshotAsync(
                    client, name, toolName, args, sidArg, noSnap, dirArg, token),
                ct);
        });

        return cmd;
    }
}
