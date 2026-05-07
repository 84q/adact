using System.CommandLine;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary><c>mousedown</c> コマンド。現在カーソル位置でマウスボタンを押下保持する (低レベル)。</summary>
internal static class MousedownCommand
{
    /// <summary>mousedown サブコマンドを構築する。</summary>
    /// <returns>System.CommandLine 用 <see cref="Command"/>。</returns>
    public static Command Build()
    {
        var button = OperationOptions.Button();

        var cmd = new Command("mousedown", "Press and hold a mouse button at the current cursor position.");
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
                    var r = await client.CallToolAsync("windows_mouse_down", args, token).ConfigureAwait(false);
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
