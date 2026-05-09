using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class ScreenshotCommand
{
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

    /// <param name="ct">cancellation token。</param>
    /// <param name="client">The connected MCP client.</param>
    /// <param name="refValue">The optional element reference to capture.</param>
    /// <param name="outValue">The optional output PNG path.</param>
    /// <param name="sidValue">The optional session id to capture when no ref is supplied.</param>
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

        var result = await client.CallToolAsync("adact_screenshot", args, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var json = McpResponse.GetJson(result);
        var fields = CliOutput.JsonObjectToFields(json, "sessionId", "path", "width", "height");
        CliOutput.WriteYamlSuccess(metaFields: null, fields);
        return ExitCodes.Success;
    }
}
