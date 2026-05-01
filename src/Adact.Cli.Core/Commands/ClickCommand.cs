using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>click</c> コマンド。Element Ref で指定された要素をクリックし、成功時に snapshot を自動取得する。
/// Phase 8 で <c>--button</c>, <c>--count</c>, <c>--modifier</c>, <c>--position</c> を追加。
/// </summary>
internal static class ClickCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>click サブコマンド。</returns>
    public static Command Build()
    {
        var refArg = new Argument<string>("ref")
        {
            Description = "Element Ref ID like 's1e7' (from snapshot).",
        };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();
        var button = OperationOptions.Button();
        var count = OperationOptions.Count();
        var modifiers = OperationOptions.Modifiers();
        var position = new Option<string?>("--position")
        {
            Description = "Click point relative to the element's bounding-rectangle top-left, as 'x,y'. Defaults to the center.",
        };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("click", "Click an element identified by an Element Ref.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(button);
        cmd.Options.Add(count);
        cmd.Options.Add(modifiers);
        cmd.Options.Add(position);
        cmd.Options.Add(server);

        cmd.SetAction((parseResult, ct) =>
        {
            var refValue = parseResult.GetValue(refArg);
            var noSnap = parseResult.GetValue(noSnapshot);
            var dirArg = parseResult.GetValue(snapshotDir);
            var btn = parseResult.GetValue(button);
            var cnt = parseResult.GetValue(count);
            var mods = parseResult.GetValue(modifiers);
            var posStr = parseResult.GetValue(position);
            var serverArg = parseResult.GetValue(server);

            if (!RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }
            if (!OperationOptions.ValidateButton(btn, out var btnErr))
                return Task.FromResult(OperationOptions.ReportUserError(btnErr));
            if (cnt is { } c && c < 1)
                return Task.FromResult(OperationOptions.ReportUserError("--count must be >= 1."));
            if (!OperationOptions.ValidateModifiers(mods, out var modErr))
                return Task.FromResult(OperationOptions.ReportUserError(modErr));
            if (!OperationOptions.TryParsePosition(posStr, out var px, out var py))
                return Task.FromResult(OperationOptions.ReportUserError(
                    $"--position must be 'x,y' integers, got '{posStr}'."));

            var args = new Dictionary<string, object?> { ["ref"] = refValue };
            if (!string.IsNullOrEmpty(btn)) args["button"] = btn;
            if (cnt is not null) args["count"] = cnt;
            if (mods is { Length: > 0 }) args["modifiers"] = mods;
            if (px is not null) args["positionX"] = px;
            if (py is not null) args["positionY"] = py;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                    client, "windows_click", args, refValue!, noSnap, dirArg, token),
                ct);
        });

        return cmd;
    }
}
