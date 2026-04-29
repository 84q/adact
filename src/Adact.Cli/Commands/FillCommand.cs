using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>fill</c> コマンド。Element Ref で指定された入力コントロールにテキストを上書きし、成功時は snapshot を自動取得する。
/// </summary>
internal static class FillCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>fill サブコマンド。</returns>
    public static Command Build()
    {
        var refArg = new Argument<string>("ref")
        {
            Description = "Element Ref ID like 's1e7' (from snapshot).",
        };
        var textArg = new Argument<string>("text")
        {
            Description = "Text value to fill into the element.",
        };
        var noSnapshot = new Option<bool>("--no-snapshot") { Description = "Do not capture a snapshot after the action." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("fill", "Fill (overwrite) an input element with text.");
        cmd.Arguments.Add(refArg);
        cmd.Arguments.Add(textArg);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var refValue = parseResult.GetValue(refArg);
            var textValue = parseResult.GetValue(textArg);
            var noSnap = parseResult.GetValue(noSnapshot);
            var dirArg = parseResult.GetValue(snapshotDir);
            var serverArg = parseResult.GetValue(server);

            if (!RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }

            if (textValue is null)
            {
                CliError.Write(ErrorCodes.InvalidArgument, "text argument is required.");
                return Task.FromResult(ExitCodes.UserError);
            }

            var args = new Dictionary<string, object?>
            {
                ["ref"] = refValue,
                ["value"] = textValue,
            };
            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                    client, "windows_fill", args, refValue!, noSnap, dirArg, token),
                ct);
        });

        return cmd;
    }
}
