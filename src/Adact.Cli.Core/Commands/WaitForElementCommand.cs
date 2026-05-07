using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>wait-for-element</c> コマンド (設計 022 §6 / §7)。指定 element ref または検索条件にマッチする要素が指定 state を満たすまで待機する。
/// auto-snapshot は発火しない。
/// </summary>
internal static class WaitForElementCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>wait-for-element サブコマンド。</returns>
    public static Command Build()
    {
        var refOpt = new Option<string?>("--ref")
        {
            Description = "Element ref like 's1e7'. Mutually exclusive with --name/--control-type/--automation-id/--class-name.",
        };
        var nameOpt = new Option<string?>("--name") { Description = "Search condition: UIA Name exact match (case-insensitive)." };
        var controlTypeOpt = new Option<string?>("--control-type") { Description = "Search condition: ControlType exact match (e.g. 'Button')." };
        var autoIdOpt = new Option<string?>("--automation-id") { Description = "Search condition: AutomationId exact match." };
        var classNameOpt = new Option<string?>("--class-name") { Description = "Search condition: ClassName exact match." };
        var stateOpt = new Option<string?>("--state")
        {
            Description = "Target state: attached, detached, visible, hidden, enabled, disabled. Defaults to 'visible'.",
        };
        var timeoutOpt = new Option<int?>("--timeout") { Description = "Polling timeout in milliseconds. Defaults to 5000." };
        var sidOpt = new Option<string?>("--sid") { Description = "Target session ID (default: active session). Only used with search conditions." };

        var cmd = new Command("wait-for-element", "Wait until an element reaches the given state. Specify either --ref or search conditions (mutually exclusive).");
        cmd.Options.Add(refOpt);
        cmd.Options.Add(nameOpt);
        cmd.Options.Add(controlTypeOpt);
        cmd.Options.Add(autoIdOpt);
        cmd.Options.Add(classNameOpt);
        cmd.Options.Add(stateOpt);
        cmd.Options.Add(timeoutOpt);
        cmd.Options.Add(sidOpt);

        cmd.SetAction((parseResult, ct) =>
        {
            var refValue = parseResult.GetValue(refOpt);
            var nameValue = parseResult.GetValue(nameOpt);
            var controlTypeValue = parseResult.GetValue(controlTypeOpt);
            var autoIdValue = parseResult.GetValue(autoIdOpt);
            var classNameValue = parseResult.GetValue(classNameOpt);
            var stateValue = parseResult.GetValue(stateOpt);
            var timeoutValue = parseResult.GetValue(timeoutOpt);
            var sidValue = parseResult.GetValue(sidOpt);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            var (errorCode, errorMessage) = ValidateArgs(
                refValue, nameValue, controlTypeValue, autoIdValue, classNameValue, stateValue, timeoutValue);
            if (errorCode is not null)
            {
                CliError.Write(errorCode, errorMessage ?? "invalid arguments.");
                return Task.FromResult(ExitCodes.UserError);
            }

            var args = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(refValue)) args["ref"] = refValue;
            if (!string.IsNullOrEmpty(nameValue)) args["name"] = nameValue;
            if (!string.IsNullOrEmpty(controlTypeValue)) args["controlType"] = controlTypeValue;
            if (!string.IsNullOrEmpty(autoIdValue)) args["automationId"] = autoIdValue;
            if (!string.IsNullOrEmpty(classNameValue)) args["className"] = classNameValue;
            if (!string.IsNullOrEmpty(stateValue)) args["state"] = stateValue;
            if (timeoutValue is { } t) args["timeoutMs"] = t;
            if (!string.IsNullOrEmpty(sidValue)) args["sessionId"] = sidValue;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => ExecuteAsync(client, args, token),
                ct);
        });

        return cmd;
    }

    /// <summary>引数バリデーション。Unit テストから直接呼ぶための internal API。</summary>
    /// <param name="refValue">--ref。</param>
    /// <param name="name">--name。</param>
    /// <param name="controlType">--control-type。</param>
    /// <param name="automationId">--automation-id。</param>
    /// <param name="className">--class-name。</param>
    /// <param name="state">--state。</param>
    /// <param name="timeoutMs">--timeout。</param>
    /// <returns>(エラーコード, メッセージ) のタプル。OK なら (null, null)。</returns>
    internal static (string? errorCode, string? errorMessage) ValidateArgs(
        string? refValue,
        string? name,
        string? controlType,
        string? automationId,
        string? className,
        string? state,
        int? timeoutMs)
    {
        var hasRef = !string.IsNullOrEmpty(refValue);
        var hasQuery = !string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(controlType)
            || !string.IsNullOrEmpty(automationId) || !string.IsNullOrEmpty(className);

        if (hasRef && hasQuery)
        {
            return (ErrorCodes.InvalidArgument,
                "--ref and search conditions (--name/--control-type/--automation-id/--class-name) are mutually exclusive.");
        }
        if (!hasRef && !hasQuery)
        {
            return (ErrorCodes.InvalidArgument,
                "Specify either --ref or at least one of --name/--control-type/--automation-id/--class-name.");
        }
        if (hasRef && !RefValidator.IsElementRef(refValue))
        {
            return (ErrorCodes.InvalidRefFormat,
                $"--ref must be in 's<sid>e<eid>' form, got '{refValue}'.");
        }
        if (timeoutMs is { } t && t <= 0)
        {
            return (ErrorCodes.InvalidArgument, "--timeout must be > 0.");
        }
        if (!string.IsNullOrEmpty(state)
            && !WaitForStateParser.TryParse(state, out _))
        {
            return (ErrorCodes.InvalidArgument,
                $"--state '{state}' is not one of: {WaitForStateParser.AllowedValues}.");
        }
        return (null, null);
    }

    private static async Task<int> ExecuteAsync(
        IAdactMcpClient client,
        Dictionary<string, object?> args,
        CancellationToken ct)
    {
        var result = await client.CallToolAsync("windows_wait_for", args, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var json = McpResponse.GetJson(result);
        CliOutput.WriteYamlSuccess(metaFields: null, CliOutput.JsonObjectToFields(json, "sessionId", "ref", "state"));
        return ExitCodes.Success;
    }
}
