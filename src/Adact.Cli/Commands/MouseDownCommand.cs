using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>mouse-down</c> コマンド。target 位置でマウスボタンを押下保持する (低レベル)。</summary>
internal static class MouseDownCommand
{
    /// <summary>mouse-down サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var targetArg = new Argument<string>("target")
        {
            Description = "Either an element ref or 'x,y' coordinates.",
        };
        var button = OperationOptions.Button();
        var server = CommandHelpers.CreateServerOption();

        var cmd = new Command("mouse-down", "Press and hold a mouse button at the target.");
        cmd.Arguments.Add(targetArg);
        cmd.Options.Add(button);
        cmd.Options.Add(server);

        cmd.SetAction((pr, ct) =>
        {
            var target = pr.GetValue(targetArg);
            var btn = pr.GetValue(button);
            var serverArg = pr.GetValue(server);
            if (string.IsNullOrEmpty(target))
                return Task.FromResult(OperationOptions.ReportUserError("target argument is required."));
            if (!OperationOptions.ValidateButton(btn, out var be))
                return Task.FromResult(OperationOptions.ReportUserError(be));

            var args = new Dictionary<string, object?> { ["target"] = target };
            if (!string.IsNullOrEmpty(btn)) args["button"] = btn;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                async (client, token) =>
                {
                    var r = await client.CallToolAsync("windows_mouse_down", args, token).ConfigureAwait(false);
                    return McpResponse.TryReportError(r) ?? ExitCodes.Success;
                },
                ct);
        });
        return cmd;
    }
}
