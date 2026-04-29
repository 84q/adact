using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>mouse-wheel</c> コマンド。target 位置でマウスホイールをスクロールする (auto-snapshot あり)。
/// target が ref 形式のときはその session に対し、座標形式のときは active session に対し snapshot を取得する。</summary>
internal static class MouseWheelCommand
{
    /// <summary>mouse-wheel サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var targetArg = new Argument<string>("target")
        {
            Description = "Either an element ref or 'x,y' coordinates.",
        };
        var deltaY = new Option<int>("--delta-y")
        {
            Description = "Vertical scroll amount in notches (positive = down).",
        };
        var deltaX = new Option<int>("--delta-x")
        {
            Description = "Horizontal scroll amount in notches (positive = right).",
        };
        var noSnapshot = OperationOptions.NoSnapshot();
        var snapshotDir = OperationOptions.SnapshotDir();
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("mouse-wheel", "Scroll the mouse wheel at a target.");
        cmd.Arguments.Add(targetArg);
        cmd.Options.Add(deltaY);
        cmd.Options.Add(deltaX);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(server);

        cmd.SetAction((pr, ct) =>
        {
            var target = pr.GetValue(targetArg);
            var dy = pr.GetValue(deltaY);
            var dx = pr.GetValue(deltaX);
            var noSnap = pr.GetValue(noSnapshot);
            var dirArg = pr.GetValue(snapshotDir);
            var serverArg = pr.GetValue(server);
            if (string.IsNullOrEmpty(target))
                return Task.FromResult(OperationOptions.ReportUserError("target argument is required."));

            var args = new Dictionary<string, object?>
            {
                ["target"] = target,
                ["deltaY"] = dy,
                ["deltaX"] = dx,
            };

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                async (client, token) =>
                {
                    var r = await client.CallToolAsync("windows_mouse_wheel", args, token).ConfigureAwait(false);
                    var err = McpResponse.TryReportError(r);
                    if (err is { } code) return code;
                    if (noSnap) return ExitCodes.Success;
                    var sid = RefValidator.IsElementRef(target) ? RefValidator.ExtractSessionId(target) : null;
                    return await CommandHelpers.WriteSnapshotResultAsync(client, sid, dirArg, token).ConfigureAwait(false);
                },
                ct);
        });
        return cmd;
    }
}
