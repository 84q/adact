using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>mouse-move</c> コマンド。要素 ref または "x,y" 座標へカーソルを移動する (低レベル: auto-snapshot なし)。</summary>
internal static class MouseMoveCommand
{
    /// <summary>mouse-move サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var targetArg = new Argument<string>("target")
        {
            Description = "Either an element ref ('s<sid>e<eid>') or screen coordinates ('x,y').",
        };
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("mouse-move", "Move the mouse cursor to a target (element ref or 'x,y').");
        cmd.Arguments.Add(targetArg);
        cmd.Options.Add(server);

        cmd.SetAction((pr, ct) =>
        {
            var target = pr.GetValue(targetArg);
            var serverArg = pr.GetValue(server);
            if (string.IsNullOrEmpty(target))
                return Task.FromResult(OperationOptions.ReportUserError("target argument is required."));

            var args = new Dictionary<string, object?> { ["target"] = target };
            return CommandHelpers.RunWithClientAsync(
                serverArg,
                async (client, token) =>
                {
                    var r = await client.CallToolAsync("windows_mouse_move", args, token).ConfigureAwait(false);
                    return McpResponse.TryReportError(r) ?? ExitCodes.Success;
                },
                ct);
        });
        return cmd;
    }
}
