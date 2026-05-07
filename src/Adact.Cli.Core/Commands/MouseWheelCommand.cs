using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>mousewheel</c> コマンド。現在カーソル位置でマウスホイールをスクロールする (低レベル: auto-snapshot なし)。</summary>
internal static class MousewheelCommand
{
    /// <summary>mousewheel サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var deltaY = new Option<int>("--delta-y")
        {
            Description = "Vertical scroll amount in notches (positive = down).",
        };
        var deltaX = new Option<int>("--delta-x")
        {
            Description = "Horizontal scroll amount in notches (positive = right).",
        };
        var cmd = new Command("mousewheel", "Scroll the mouse wheel at the current cursor position.");
        cmd.Options.Add(deltaY);
        cmd.Options.Add(deltaX);

        cmd.SetAction((pr, ct) =>
        {
            var dy = pr.GetValue(deltaY);
            var dx = pr.GetValue(deltaX);
            var serverArg = pr.GetValue(CommandHelpers.ServerOption);

            var args = new Dictionary<string, object?>
            {
                ["deltaY"] = dy,
                ["deltaX"] = dx,
            };

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                async (client, token) =>
                {
                    var r = await client.CallToolAsync("adact_mousewheel", args, token).ConfigureAwait(false);
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
