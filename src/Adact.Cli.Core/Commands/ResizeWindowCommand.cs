using System.CommandLine;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>resize-window</c> コマンド。アタッチ済みウィンドウのサイズを変更し、成功時に snapshot を自動取得する。
/// width / height のどちらか片方のみ指定可。省略側は現在値を維持する。
/// </summary>
internal static class ResizeWindowCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>resize-window サブコマンド。</returns>
    public static Command Build()
    {
        var width = new Option<int?>("--width") { Description = "New window width in pixels (must be > 0)." };
        var height = new Option<int?>("--height") { Description = "New window height in pixels (must be > 0)." };
        var sid = new Argument<string?>("sid") { Arity = ArgumentArity.ZeroOrOne, Description = "Target session ID (default: active session)." };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();

        var cmd = new Command("resize-window", "Resize the attached window via UIA TransformPattern.Resize. At least one of --width/--height required.");
        cmd.Options.Add(width);
        cmd.Options.Add(height);
        cmd.Arguments.Add(sid);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);

        cmd.SetAction((parseResult, ct) =>
        {
            var w = parseResult.GetValue(width);
            var h = parseResult.GetValue(height);
            var sidArg = parseResult.GetValue(sid);
            var noSnap = parseResult.GetValue(noSnapshot);
            var dirArg = parseResult.GetValue(snapshotDir);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            if (w is null && h is null)
                return Task.FromResult(OperationOptions.ReportUserError("At least one of --width or --height must be specified."));
            if (w is <= 0)
                return Task.FromResult(OperationOptions.ReportUserError("--width must be a positive integer."));
            if (h is <= 0)
                return Task.FromResult(OperationOptions.ReportUserError("--height must be a positive integer."));

            var args = new Dictionary<string, object?>();
            if (w is not null) args["width"] = w.Value;
            if (h is not null) args["height"] = h.Value;
            if (!string.IsNullOrEmpty(sidArg)) args["sessionId"] = sidArg;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => CommandHelpers.RunSessionOperationAndAutoSnapshotAsync(
                    client, "resize", "adact_resize_window", args, sidArg, noSnap, dirArg, token),
                ct);
        });

        return cmd;
    }
}
