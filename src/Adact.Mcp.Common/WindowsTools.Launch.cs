using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    /// <summary>UWP モードを示す入力プレフィックス (case-insensitive)。設計 024 §2。</summary>
    private const string LaunchUwpPrefix = "shell:AppsFolder\\";

    /// <summary>
    /// 実行ファイルを起動する。Win32 / .NET は <c>Process.Start</c>、UWP は <c>shell:AppsFolder\&lt;AUMID&gt;</c> 形式で
    /// 受け取り <see cref="Adact.Engine.UiaEngine.LaunchAsync"/> 経由で起動する。設計 024 §4。
    /// </summary>
    /// <param name="executable">実行ファイルパス、PATH 探索対象の名前、もしくは <c>shell:AppsFolder\&lt;AUMID&gt;</c>。</param>
    /// <param name="args">コマンドライン引数 (任意)。</param>
    /// <param name="cwd">作業ディレクトリ (任意)。UWP モードでは指定不可。</param>
    /// <param name="env">環境変数の上書き / 追加 (任意)。UWP モードでは指定不可。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は <c>{ pid, processName, executablePath }</c> JSON。失敗時は LAUNCH_FAILED / INVALID_ARGUMENT。</returns>
    [McpServerTool(Name = "windows_launch")]
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

        // UWP + cwd/env の併用は早期検出 (Engine 側でも検証するが、明確なエラーコードを返すため CLI 側でも一段)。
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
        catch (Exception ex) { return MapOrLog(ex, "windows_launch"); }
    }
}
