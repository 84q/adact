using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>type</c> コマンド。要素にフォーカスしテキストを 1 文字ずつ Type する (auto-snapshot あり)。</summary>
internal static class TypeCommand
{
    /// <summary>type サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var refArg = new Argument<string>("ref") { Description = "Element Ref ID like 's1e7'." };
        var textArg = new Argument<string>("text") { Description = "Text to type." };
        var delay = new Option<int?>("--delay-ms")
        {
            Description = "Delay between characters in milliseconds (>= 0). 0 means no delay.",
        };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();

        var cmd = new Command("type", "Type text into an element character by character.");
        cmd.Arguments.Add(refArg);
        cmd.Arguments.Add(textArg);
        cmd.Options.Add(delay);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);

        cmd.SetAction((pr, ct) =>
        {
            var refValue = pr.GetValue(refArg);
            var text = pr.GetValue(textArg);
            var d = pr.GetValue(delay);
            var noSnap = pr.GetValue(noSnapshot);
            var dirArg = pr.GetValue(snapshotDir);
            var serverArg = pr.GetValue(CommandHelpers.ServerOption);

            if (!RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }
            if (text is null)
                return Task.FromResult(OperationOptions.ReportUserError("text argument is required."));
            if (d is { } dv && dv < 0)
                return Task.FromResult(OperationOptions.ReportUserError("--delay-ms must be >= 0."));

            var args = new Dictionary<string, object?>
            {
                ["ref"] = refValue,
                ["text"] = text,
            };
            if (d is not null) args["delayMs"] = d;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                    client, "windows_type", args, refValue!, noSnap, dirArg, token),
                ct);
        });
        return cmd;
    }
}
