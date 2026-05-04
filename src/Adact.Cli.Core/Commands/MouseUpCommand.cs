using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>mouse-up</c> コマンド。target 位置でマウスボタンを解放する (低レベル)。</summary>
internal static class MouseUpCommand
{
    /// <summary>mouse-up サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var targetArg = new Argument<string>("target")
        {
            Description = "Either an element ref or 'x,y' coordinates.",
        };
        var button = OperationOptions.Button();

        var cmd = new Command("mouse-up", "Release a mouse button at the target.");
        cmd.Arguments.Add(targetArg);
        cmd.Options.Add(button);

        cmd.SetAction((pr, ct) =>
        {
            var target = pr.GetValue(targetArg);
            var btn = pr.GetValue(button);
            var serverArg = pr.GetValue(CommandHelpers.ServerOption);
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
                    var r = await client.CallToolAsync("windows_mouse_up", args, token).ConfigureAwait(false);
                    var err = McpResponse.TryReportError(r);
                    return err ?? CommandHelpers.WriteToolSuccess("mouse-up", [CliOutput.Field("target", target), CliOutput.Field("button", btn)]);
                },
                ct);
        });
        return cmd;
    }
}
