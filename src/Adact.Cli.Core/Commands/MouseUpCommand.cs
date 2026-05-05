using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>mouse-up</c> コマンド。現在カーソル位置でマウスボタンを解放する (低レベル)。</summary>
internal static class MouseUpCommand
{
    /// <summary>mouse-up サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var button = OperationOptions.Button();

        var cmd = new Command("mouse-up", "Release a mouse button at the current cursor position.");
        cmd.Options.Add(button);

        cmd.SetAction((pr, ct) =>
        {
            var btn = pr.GetValue(button);
            var serverArg = pr.GetValue(CommandHelpers.ServerOption);
            if (!OperationOptions.ValidateButton(btn, out var be))
                return Task.FromResult(OperationOptions.ReportUserError(be));

            var args = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(btn)) args["button"] = btn;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                async (client, token) =>
                {
                    var r = await client.CallToolAsync("windows_mouse_up", args, token).ConfigureAwait(false);
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
