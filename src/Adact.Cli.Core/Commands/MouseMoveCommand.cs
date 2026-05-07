using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>mousemove</c> コマンド。"x,y" 座標へカーソルを移動する (低レベル: auto-snapshot なし)。</summary>
internal static class MousemoveCommand
{
    /// <summary>mousemove サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var targetArg = new Argument<string>("target")
        {
            Description = "Absolute screen coordinates ('x,y').",
        };

        var cmd = new Command("mousemove", "Move the mouse cursor to absolute screen coordinates ('x,y').");
        cmd.Arguments.Add(targetArg);

        cmd.SetAction((pr, ct) =>
        {
            var target = pr.GetValue(targetArg);
            var serverArg = pr.GetValue(CommandHelpers.ServerOption);
            if (string.IsNullOrEmpty(target))
                return Task.FromResult(OperationOptions.ReportUserError("target argument is required."));

            var args = new Dictionary<string, object?> { ["target"] = target };
            return CommandHelpers.RunWithClientAsync(
                serverArg,
                async (client, token) =>
                {
                    var r = await client.CallToolAsync("windows_mouse_move", args, token).ConfigureAwait(false);
                    var err = McpResponse.TryReportError(r);
                    if (err is { } code) return code;
                    CliOutput.WriteEmptySuccess();
                    return ExitCodes.Success;
                },
                ct);
        });
        return cmd;
    }
}
