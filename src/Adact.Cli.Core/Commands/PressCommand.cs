using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>press</c> コマンド。キーコンボを送出する (auto-snapshot あり、ref 指定時はそれを focus)。</summary>
internal static class PressCommand
{
    /// <summary>press サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var keyArg = new Argument<string>("key")
        {
            Description = "Key combo such as 'Enter', 'F5', or 'Ctrl+Shift+E'.",
        };
        var refOption = new Option<string?>("--ref")
        {
            Description = "Optional element ref to focus before pressing.",
        };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();

        var cmd = new Command("press", "Press a key combo (e.g. 'Ctrl+C').");
        cmd.Arguments.Add(keyArg);
        cmd.Options.Add(refOption);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);

        cmd.SetAction((pr, ct) =>
        {
            var key = pr.GetValue(keyArg);
            var refValue = pr.GetValue(refOption);
            var noSnap = pr.GetValue(noSnapshot);
            var dirArg = pr.GetValue(snapshotDir);
            var serverArg = pr.GetValue(CommandHelpers.ServerOption);
            if (string.IsNullOrEmpty(key))
                return Task.FromResult(OperationOptions.ReportUserError("key argument is required."));
            if (refValue is not null && !RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"--ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }

            var args = new Dictionary<string, object?> { ["key"] = key };
            if (refValue is not null) args["ref"] = refValue;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                async (client, token) =>
                {
                    var r = await client.CallToolAsync("windows_press", args, token).ConfigureAwait(false);
                    var err = McpResponse.TryReportError(r);
                    if (err is { } code) return code;
                    if (noSnap) return ExitCodes.Success;
                    var sid = refValue is not null ? RefValidator.ExtractSessionId(refValue) : null;
                    return await CommandHelpers.WriteSnapshotResultAsync(client, sid, dirArg, token).ConfigureAwait(false);
                },
                ct);
        });
        return cmd;
    }
}
