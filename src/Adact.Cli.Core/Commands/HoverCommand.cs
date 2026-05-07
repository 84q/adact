using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>hover</c> コマンド。要素にカーソルを移動する (auto-snapshot あり)。</summary>
internal static class HoverCommand
{
    /// <summary>hover サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var refArg = new Argument<string>("ref") { Description = "Element Ref ID like 's1e7'." };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();
        var modifiers = OperationOptions.Modifiers();
        var position = new Option<string?>("--position")
        {
            Description = "Hover point relative to element top-left, as 'x,y'. Defaults to center.",
        };

        var cmd = new Command("hover", "Move the mouse cursor over an element.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(modifiers);
        cmd.Options.Add(position);

        cmd.SetAction((pr, ct) =>
        {
            var refValue = pr.GetValue(refArg);
            var noSnap = pr.GetValue(noSnapshot);
            var dirArg = pr.GetValue(snapshotDir);
            var mods = pr.GetValue(modifiers);
            var posStr = pr.GetValue(position);
            var serverArg = pr.GetValue(CommandHelpers.ServerOption);

            if (!RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }
            if (!OperationOptions.ValidateModifiers(mods, out var me))
                return Task.FromResult(OperationOptions.ReportUserError(me));
            if (!OperationOptions.TryParsePosition(posStr, out var px, out var py))
                return Task.FromResult(OperationOptions.ReportUserError(
                    $"--position must be 'x,y', got '{posStr}'."));

            var args = new Dictionary<string, object?> { ["ref"] = refValue };
            if (mods is { Length: > 0 }) args["modifiers"] = mods;
            if (px is not null) args["positionX"] = px;
            if (py is not null) args["positionY"] = py;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                    client, "hover", "adact_hover", args, refValue!, noSnap, dirArg, token),
                ct);
        });
        return cmd;
    }
}
