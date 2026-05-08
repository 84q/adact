using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class SelectCommand
{
    public static Command Build()
    {
        var refArg = new Argument<string>("ref")
        {
            Description = "Element Ref ID of the container (List, ComboBox).",
        };
        var name = new Option<string[]>("--name") { Description = "Name(s) of the child item(s) to select.", AllowMultipleArgumentsPerToken = true };
        var index = new Option<int[]>("--index") { Description = "0-based index(es) of the child item(s) to select.", AllowMultipleArgumentsPerToken = true };
        var itemRef = new Option<string[]>("--item-ref") { Description = "Element ref(s) of the child ListItem(s) to select.", AllowMultipleArgumentsPerToken = true };
        var addFlag = new Option<bool>("--add") { Description = "Add to existing selection instead of replacing it." };
        var removeFlag = new Option<bool>("--remove") { Description = "Remove from existing selection." };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();

        var cmd = new Command("select", "Select item(s) in a list/combobox by name, index, or item-ref.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(name);
        cmd.Options.Add(index);
        cmd.Options.Add(itemRef);
        cmd.Options.Add(addFlag);
        cmd.Options.Add(removeFlag);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);

        cmd.SetAction((pr, ct) =>
        {
            var refValue = pr.GetValue(refArg);
            var nameVal = pr.GetValue(name) ?? [];
            var indexVal = pr.GetValue(index) ?? [];
            var itemRefVal = pr.GetValue(itemRef) ?? [];
            var addVal = pr.GetValue(addFlag);
            var removeVal = pr.GetValue(removeFlag);
            var noSnap = pr.GetValue(noSnapshot);
            var dirArg = pr.GetValue(snapshotDir);
            var serverArg = pr.GetValue(CommandHelpers.ServerOption);

            if (!RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }

            if (addVal && removeVal)
                return Task.FromResult(OperationOptions.ReportUserError(
                    "Cannot specify both --add and --remove."));

            int kindCount = (nameVal.Length > 0 ? 1 : 0)
                + (indexVal.Length > 0 ? 1 : 0)
                + (itemRefVal.Length > 0 ? 1 : 0);
            if (kindCount == 0)
                return Task.FromResult(OperationOptions.ReportUserError(
                    "Provide at least one of --name, --index, or --item-ref."));
            if (kindCount > 1)
                return Task.FromResult(OperationOptions.ReportUserError(
                    "Only one kind of selector (--name, --index, or --item-ref) may be specified."));

            foreach (var ir in itemRefVal)
            {
                if (!RefValidator.IsElementRef(ir))
                {
                    CliError.Write(ErrorCodes.InvalidRefFormat,
                        $"--item-ref must be in 's<sid>e<eid>' form, got '{ir}'.");
                    return Task.FromResult(ExitCodes.UserError);
                }
            }

            var args = new Dictionary<string, object?> { ["ref"] = refValue };
            if (nameVal.Length > 0) args["name"] = nameVal;
            if (indexVal.Length > 0) args["index"] = indexVal;
            if (itemRefVal.Length > 0) args["itemRef"] = itemRefVal;
            if (addVal) args["add"] = true;
            if (removeVal) args["remove"] = true;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                    client, "select", "adact_select", args, refValue!, noSnap, dirArg, token),
                ct);
        });
        return cmd;
    }
}
