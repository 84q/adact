using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// "ref のみを引数に取り、ref 以外のオプションを持たない" ref-based コマンドを共通化するビルダ。
/// auto-snapshot あり / なしを切り替える。
/// </summary>
internal static class RefOnlyCommandBuilder
{
    /// <summary>共通 ref-only コマンドを構築する。</summary>
    /// <param name="name">コマンド名 (kebab-case)。</param>
    /// <param name="description">help 用説明文。</param>
    /// <param name="toolName">呼び出す MCP ツール名 (例 <c>windows_check</c>)。</param>
    /// <param name="autoSnapshot">true なら成功時に auto-snapshot を取得する。</param>
    /// <returns>構築された <see cref="Command"/>。</returns>
    public static Command Build(string name, string description, string toolName, bool autoSnapshot)
    {
        var refArg = new Argument<string>("ref") { Description = "Element Ref ID like 's1e7'." };

        var cmd = new Command(name, description);
        cmd.Arguments.Add(refArg);

        Option<bool>? noSnapshot = null;
        Option<string?>? snapshotDir = null;
        if (autoSnapshot)
        {
            noSnapshot = OperationOptions.NoSnapshot();
            snapshotDir = OperationOptions.SnapshotDir();
            cmd.Options.Add(noSnapshot);
            cmd.Options.Add(snapshotDir);
        }

        cmd.SetAction((pr, ct) =>
        {
            var refValue = pr.GetValue(refArg);
            var serverArg = pr.GetValue(CommandHelpers.ServerOption);
            if (!RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }

            var args = new Dictionary<string, object?> { ["ref"] = refValue };
            if (autoSnapshot)
            {
                var noSnap = pr.GetValue(noSnapshot!);
                var dirArg = pr.GetValue(snapshotDir!);
                return CommandHelpers.RunWithClientAsync(
                    serverArg,
                    (client, token) => CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                        client, name, toolName, args, refValue!, noSnap, dirArg, token),
                    ct);
            }
            return CommandHelpers.RunWithClientAsync(
                serverArg,
                async (client, token) =>
                {
                    var r = await client.CallToolAsync(toolName, args, token).ConfigureAwait(false);
                    var err = McpResponse.TryReportError(r);
                    return err ?? CommandHelpers.WriteToolSuccess(name, [CliOutput.Field("target", refValue)]);
                },
                ct);
        });
        return cmd;
    }
}
