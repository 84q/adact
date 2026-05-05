using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>screenshot</c> コマンド (設計 022 §6 / §10)。アタッチ済みウィンドウまたは指定要素を PNG 保存する。
/// auto-snapshot は発火しない。
/// </summary>
internal static class ScreenshotCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>screenshot サブコマンド。</returns>
    public static Command Build()
    {
        var targetArg = new Argument<string?>("target")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Optional session/ref target. 's<sid>e<eid>' is treated as ref; otherwise treated as session ID.",
        };
        var outOpt = new Option<string?>("--out")
        {
            Description = "Output PNG path (default '.adact/screenshot-<sid>-<UTC ts>.png').",
        };
        var cmd = new Command("screenshot", "Capture a PNG screenshot of the attached window or a specific element.");
        cmd.Arguments.Add(targetArg);
        cmd.Options.Add(outOpt);

        cmd.SetAction((parseResult, ct) =>
        {
            var targetValue = parseResult.GetValue(targetArg);
            var outValue = parseResult.GetValue(outOpt);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            var refValue = !string.IsNullOrEmpty(targetValue) && RefValidator.IsElementRef(targetValue)
                ? targetValue
                : null;
            var sidValue = refValue is null ? targetValue : null;

            if (!string.IsNullOrEmpty(outValue)
                && !string.Equals(Path.GetExtension(outValue), ".png", StringComparison.OrdinalIgnoreCase))
            {
                CliError.Write(ErrorCodes.InvalidArgument,
                    $"--out must end with '.png' (got '{outValue}'). Screenshot format is PNG-only.");
                return Task.FromResult(ExitCodes.UserError);
            }

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => ExecuteAsync(client, refValue, outValue, sidValue, token),
                ct);
        });

        return cmd;
    }

    /// <summary><c>windows_screenshot</c> を呼び、結果 JSON を stdout に出力する。</summary>
    /// <param name="client">接続済み MCP クライアント。</param>
    /// <param name="refValue">ref 値。null/空ならウィンドウ全体。</param>
    /// <param name="outValue"><c>--out</c> の値。null/空ならデフォルトパス。</param>
    /// <param name="sidValue">session id 値。<paramref name="refValue"/> 未指定時のみ使われる。</param>
    /// <param name="ct">cancellation token。</param>
    /// <returns>exit code。</returns>
    private static async Task<int> ExecuteAsync(
        IAdactMcpClient client,
        string? refValue,
        string? outValue,
        string? sidValue,
        CancellationToken ct)
    {
        var args = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(refValue)) args["ref"] = refValue;
        if (!string.IsNullOrEmpty(outValue)) args["out"] = outValue;
        if (!string.IsNullOrEmpty(sidValue)) args["sessionId"] = sidValue;

        var result = await client.CallToolAsync("windows_screenshot", args, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var json = McpResponse.GetJson(result);
        var fields = CliOutput.JsonObjectToFields(json, "sessionId", "path", "width", "height");
        CliOutput.WriteYamlSuccess(metaFields: null, fields);
        return ExitCodes.Success;
    }
}
