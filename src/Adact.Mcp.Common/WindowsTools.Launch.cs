using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    private const string LaunchUwpPrefix = "shell:AppsFolder\\";

    /// <summary>
    /// </summary>
    [McpServerTool(Name = "adact_launch")]
    [Description("Start a Windows process. Use a full path or PATH-resolved name for Win32/.NET apps, or 'shell:AppsFolder\\<AUMID>' for UWP/Store apps. Returns pid only; attach is not performed.")]
    public async Task<CallToolResult> LaunchAsync(
        [Description("Executable path / PATH name / 'shell:AppsFolder\\<AUMID>'.")]
        string executable,
        [Description("Command-line arguments. Each element is passed as a single argument.")]
        string[]? args = null,
        [Description("Working directory. Not allowed for UWP launches.")]
        string? cwd = null,
        [Description("Environment variables to merge over the daemon's environment. Not allowed for UWP launches.")]
        Dictionary<string, string>? env = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, "executable must not be empty.");
        }

        var isUwp = executable.StartsWith(LaunchUwpPrefix, StringComparison.OrdinalIgnoreCase);
        if (isUwp)
        {
            if (!string.IsNullOrEmpty(cwd))
            {
                return ToolErrors.Error(ToolErrors.InvalidArgument, "cwd is unsupported with UWP launch.");
            }
            if (env is { Count: > 0 })
            {
                return ToolErrors.Error(ToolErrors.InvalidArgument, "env is unsupported with UWP launch.");
            }
        }

        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        try
        {
            var request = new LaunchRequest(
                Executable: executable,
                Arguments: args,
                WorkingDirectory: cwd,
                Environment: env);

            var result = await _store.Engine.LaunchAsync(request, ct).ConfigureAwait(false);

            var json = new JsonObject
            {
                ["pid"] = result.Pid,
                ["processName"] = result.ProcessName,
                ["executablePath"] = result.ExecutablePath,
            };
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = json.ToJsonString() }],
                StructuredContent = JsonSerializer.SerializeToElement(json),
            };
        }
        catch (ArgumentException ex)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, ex.Message);
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_launch"); }
    }
}
