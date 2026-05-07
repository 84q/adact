using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>wait-for-window</c> コマンド (設計 022 §6 / §7)。検索条件にマッチする top-level window の出現を待つ。
/// attach は行わない。auto-snapshot は発火しない。
/// </summary>
internal static class WaitForWindowCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>wait-for-window サブコマンド。</returns>
    public static Command Build()
    {
        var titleOpt = new Option<string?>("--title") { Description = "Window title regex (case-insensitive)." };
        var classNameOpt = new Option<string?>("--class-name") { Description = "Win32 ClassName regex (case-insensitive)." };
        var processNameOpt = new Option<string?>("--process-name") { Description = "Process name regex (case-insensitive, no extension)." };
        var exeOpt = new Option<string?>("--exe") { Description = "Executable full-path regex (case-insensitive)." };
        var timeoutOpt = new Option<int?>("--timeout") { Description = "Polling timeout in milliseconds. Defaults to 5000." };

        var cmd = new Command("wait-for-window", "Wait until a top-level window matching the given conditions appears. Does not attach.");
        cmd.Options.Add(titleOpt);
        cmd.Options.Add(classNameOpt);
        cmd.Options.Add(processNameOpt);
        cmd.Options.Add(exeOpt);
        cmd.Options.Add(timeoutOpt);

        cmd.SetAction((parseResult, ct) =>
        {
            var title = parseResult.GetValue(titleOpt);
            var className = parseResult.GetValue(classNameOpt);
            var processName = parseResult.GetValue(processNameOpt);
            var exe = parseResult.GetValue(exeOpt);
            var timeoutValue = parseResult.GetValue(timeoutOpt);
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            var (errorCode, errorMessage) = ValidateArgs(title, className, processName, exe, timeoutValue);
            if (errorCode is not null)
            {
                CliError.Write(errorCode, errorMessage ?? "invalid arguments.");
                return Task.FromResult(ExitCodes.UserError);
            }

            var args = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(title)) args["title"] = title;
            if (!string.IsNullOrEmpty(className)) args["className"] = className;
            if (!string.IsNullOrEmpty(processName)) args["processName"] = processName;
            if (!string.IsNullOrEmpty(exe)) args["executable"] = exe;
            if (timeoutValue is { } t) args["timeoutMs"] = t;

            return CommandHelpers.RunWithClientAsync(
                serverArg,
                (client, token) => ExecuteAsync(client, args, token),
                ct);
        });

        return cmd;
    }

    /// <summary>引数バリデーション。Unit テストから直接呼ぶための internal API。</summary>
    /// <param name="title">--title。</param>
    /// <param name="className">--class-name。</param>
    /// <param name="processName">--process-name。</param>
    /// <param name="exe">--exe。</param>
    /// <param name="timeoutMs">--timeout。</param>
    /// <returns>(エラーコード, メッセージ) のタプル。</returns>
    internal static (string? errorCode, string? errorMessage) ValidateArgs(
        string? title,
        string? className,
        string? processName,
        string? exe,
        int? timeoutMs)
    {
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(className)
            && string.IsNullOrEmpty(processName) && string.IsNullOrEmpty(exe))
        {
            return (ErrorCodes.InvalidArgument,
                "Specify at least one of --title/--class-name/--process-name/--exe.");
        }
        if (timeoutMs is { } t && t <= 0)
        {
            return (ErrorCodes.InvalidArgument, "--timeout must be > 0.");
        }
        return (null, null);
    }

    private static async Task<int> ExecuteAsync(
        IAdactMcpClient client,
        Dictionary<string, object?> args,
        CancellationToken ct)
    {
        var result = await client.CallToolAsync("adact_wait_for_window", args, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var json = McpResponse.GetJson(result);
        CliOutput.WriteYamlSuccess(
            metaFields: null,
            CliOutput.JsonObjectToFields(json, "processId", "processName", "windowTitle", "controlType", "className", "nativeWindowHandle"));
        return ExitCodes.Success;
    }
}
