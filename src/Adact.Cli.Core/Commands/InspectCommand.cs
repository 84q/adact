using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>inspect</c> コマンド (設計 022 §6 / §8)。指定 Element Ref の UIA プロパティ詳細を JSON 1 行で stdout に出力する。
/// auto-snapshot は発火しない。
/// </summary>
internal static class InspectCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>inspect サブコマンド。</returns>
    public static Command Build()
    {
        var refArg = new Argument<string>("ref")
        {
            Description = "Element Ref ID like 's1e7'.",
        };

        var cmd = new Command("inspect", "Print detailed UIA properties of the element identified by ref as a single JSON line.");
        cmd.Arguments.Add(refArg);

        cmd.SetAction((parseResult, ct) =>
        {
            var refValue = parseResult.GetValue(refArg);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            if (!RefValidator.IsElementRef(refValue))
            {
                CliError.Write(ErrorCodes.InvalidRefFormat,
                    $"ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
                return Task.FromResult(ExitCodes.UserError);
            }

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => ExecuteAsync(client, refValue!, token),
                ct);
        });

        return cmd;
    }

    /// <summary><c>windows_inspect</c> を呼び、結果 JSON を 1 行で stdout に出力する。</summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="refValue">対象 Element Ref。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    private static async Task<int> ExecuteAsync(IAdactMcpClient client, string refValue, CancellationToken ct)
    {
        var args = new Dictionary<string, object?> { ["ref"] = refValue };
        var result = await client.CallToolAsync("windows_inspect", args, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var json = McpResponse.GetJson(result);
        CliOutput.WriteYamlSuccess(
            metaFields: null,
            CliOutput.JsonObjectToFields(json, "ref", "name", "controlType", "automationId", "className", "helpText", "value", "boundingRect", "isEnabled", "isOffscreen", "isKeyboardFocusable", "hasKeyboardFocus", "patterns"));
        return ExitCodes.Success;
    }
}
