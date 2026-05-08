using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

internal static class DoubleclickCommand
{
    public static Command Build()
    {
        var refArg = new Argument<string>("ref") { Description = "Element Ref ID like 's1e7'." };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();
        var button = OperationOptions.Button();
        var modifiers = OperationOptions.Modifiers();
        var position = new Option<string?>("--position")
        {
            Description = "Click point relative to the element's top-left, as 'x,y'. Defaults to center.",
        };

        var cmd = new Command("doubleclick", "Double-click an element identified by an Element Ref.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(button);
        cmd.Options.Add(modifiers);
        cmd.Options.Add(position);

        cmd.SetAction((parseResult, ct) =>
        {
            var refValue = parseResult.GetValue(refArg);
            var noSnap = parseResult.GetValue(noSnapshot);
            var dirArg = parseResult.GetValue(snapshotDir);
            var btn = parseResult.GetValue(button);
            var mods = parseResult.GetValue(modifiers);
            var posStr = parseResult.GetValue(position);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            if (!RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }
            if (!OperationOptions.ValidateButton(btn, out var be))
                return Task.FromResult(OperationOptions.ReportUserError(be));
            if (!OperationOptions.ValidateModifiers(mods, out var me))
                return Task.FromResult(OperationOptions.ReportUserError(me));
            if (!OperationOptions.TryParsePosition(posStr, out var px, out var py))
                return Task.FromResult(OperationOptions.ReportUserError(
                    $"--position must be 'x,y', got '{posStr}'."));

            var args = new Dictionary<string, object?> { ["ref"] = refValue };
            if (!string.IsNullOrEmpty(btn)) args["button"] = btn;
            if (mods is { Length: > 0 }) args["modifiers"] = mods;
            if (px is not null) args["positionX"] = px;
            if (py is not null) args["positionY"] = py;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                    client, "doubleclick", "adact_doubleclick", args, refValue!, noSnap, dirArg, token),
                ct);
        });
        return cmd;
    }
}
