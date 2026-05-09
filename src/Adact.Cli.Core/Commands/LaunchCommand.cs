using System.CommandLine;
using System.Text.Json;

using Adact.Cli.Connection;
using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class LaunchCommand
{
    public static Command Build()
    {
        var executable = new Argument<string?>("executable")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "Executable path / PATH name / 'shell:AppsFolder\\<AUMID>'.",
        };

        var rest = new Argument<string[]>("args")
        {
            Arity = ArgumentArity.ZeroOrMore,
            Description = "Arguments to pass to the executable. Place after '--' to avoid option parsing.",
        };

        var cwd = new Option<string?>("--cwd")
        {
            Description = "Working directory. Not allowed for UWP launches.",
        };

        var env = new Option<string[]>("--env")
        {
            Description = "Environment variable in KEY=VALUE form. Repeat for multiple. Not allowed for UWP launches.",
            AllowMultipleArgumentsPerToken = false,
        };

        var cmd = new Command("launch", "Start a Windows process (Win32 / .NET / UWP) and print its pid as JSON.");
        cmd.Arguments.Add(executable);
        cmd.Arguments.Add(rest);
        cmd.Options.Add(cwd);
        cmd.Options.Add(env);

        cmd.SetAction((parseResult, ct) =>
        {
            var exe = parseResult.GetValue(executable);
            var argsArray = parseResult.GetValue(rest) ?? [];
            var cwdArg = parseResult.GetValue(cwd);
            var envArg = parseResult.GetValue(env) ?? [];
            var serverArg = parseResult.GetValue(CommandHelpers.ServerOption);

            if (string.IsNullOrEmpty(exe))
            {
                return Task.FromResult(OperationOptions.ReportUserError("executable is required."));
            }

            if (!TryParseEnv(envArg, out var envDict, out var envError))
            {
                return Task.FromResult(OperationOptions.ReportUserError(envError!));
            }

            var arguments = new Dictionary<string, object?>
            {
                ["executable"] = exe,
            };
            if (argsArray.Length > 0) arguments["args"] = argsArray;
            if (!string.IsNullOrEmpty(cwdArg)) arguments["cwd"] = cwdArg;
            if (envDict.Count > 0) arguments["env"] = envDict;

            return CommandHelpers.RunWithClientAndAutoStartAsync(
                serverArg,
                (client, token) => ExecuteAsync(client, arguments, token),
                ct);
        });

        return cmd;
    }

    internal static bool TryParseEnv(
        IReadOnlyList<string> envEntries,
        out Dictionary<string, string> result,
        out string? error)
    {
        result = new(StringComparer.Ordinal);
        error = null;

        for (var i = 0; i < envEntries.Count; i++)
        {
            var entry = envEntries[i];
            if (string.IsNullOrEmpty(entry))
            {
                error = "--env entry must not be empty.";
                return false;
            }
            var idx = entry.IndexOf('=');
            if (idx <= 0)
            {
                error = $"--env entry '{entry}' must be in KEY=VALUE form.";
                return false;
            }
            var key = entry.Substring(0, idx);
            var value = entry.Substring(idx + 1);
            result[key] = value;
        }
        return true;
    }

    /// <param name="ct">cancellation token。</param>
    /// <param name="client">The connected MCP client.</param>
    /// <param name="arguments">The launch arguments sent to the tool.</param>
    /// <returns>exit code。</returns>
    private static async Task<int> ExecuteAsync(
        IAdactMcpClient client,
        Dictionary<string, object?> arguments,
        CancellationToken ct)
    {
        var result = await client.CallToolAsync("adact_launch", arguments, ct).ConfigureAwait(false);

        var errorExit = McpResponse.TryReportError(result);
        if (errorExit is { } code) return code;

        var json = McpResponse.GetJson(result);
        CliOutput.WriteYamlSuccess(
            metaFields: null,
            CliOutput.JsonObjectToFields(json, "pid", "processName", "executablePath"));
        return ExitCodes.Success;
    }
}
