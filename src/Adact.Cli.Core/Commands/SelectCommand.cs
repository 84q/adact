using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>select</c> コマンド。List/ComboBox の選択肢を <c>--name</c> / <c>--index</c> / <c>--item-ref</c> のいずれかで選ぶ (auto-snapshot あり)。
/// </summary>
internal static class SelectCommand
{
    /// <summary>select サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var refArg = new Argument<string>("ref")
        {
            Description = "Element Ref ID of the container (List, ComboBox).",
        };
        var name = new Option<string?>("--name") { Description = "Name of the child item to select." };
        var index = new Option<int?>("--index") { Description = "0-based index of the child item to select." };
        var itemRef = new Option<string?>("--item-ref") { Description = "Element ref of the child ListItem to select." };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();

        var cmd = new Command("select", "Select an item in a list/combobox by name, index, or item-ref.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(name);
        cmd.Options.Add(index);
        cmd.Options.Add(itemRef);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);

        cmd.SetAction((pr, ct) =>
        {
            var refValue = pr.GetValue(refArg);
            var nameVal = pr.GetValue(name);
            var indexVal = pr.GetValue(index);
            var itemRefVal = pr.GetValue(itemRef);
            var noSnap = pr.GetValue(noSnapshot);
            var dirArg = pr.GetValue(snapshotDir);
            var serverArg = pr.GetValue(CommandHelpers.ServerOption);

            if (!RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }

            int specified = (nameVal is not null ? 1 : 0)
                + (indexVal.HasValue ? 1 : 0)
                + (itemRefVal is not null ? 1 : 0);
            if (specified != 1)
                return Task.FromResult(OperationOptions.ReportUserError(
                    "Provide exactly one of --name, --index, or --item-ref."));

            if (itemRefVal is not null && !RefValidator.IsElementRef(itemRefVal))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"--item-ref must be in 's<sid>e<eid>' form, got '{itemRefVal}'.");
                return Task.FromResult(ExitCodes.UserError);
            }

            var args = new Dictionary<string, object?> { ["ref"] = refValue };
            if (nameVal is not null) args["name"] = nameVal;
            if (indexVal.HasValue) args["index"] = indexVal.Value;
            if (itemRefVal is not null) args["itemRef"] = itemRefVal;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunRefOperationAndAutoSnapshotAsync(
                    client, "windows_select", args, refValue!, noSnap, dirArg, token),
                ct);
        });
        return cmd;
    }
}
