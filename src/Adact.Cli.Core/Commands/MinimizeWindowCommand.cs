using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>minimize-window</c> コマンド。アタッチ済みウィンドウを最小化し、成功時に snapshot を自動取得する。
/// </summary>
internal static class MinimizeWindowCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>minimize-window サブコマンド。</returns>
    public static Command Build()
        => WindowStateCommandBuilder.Build(
            name: "minimize-window",
            description: "Minimize the attached window via UIA WindowPattern.SetWindowVisualState(Minimized).",
            toolName: "windows_minimize");
}
