using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>maximize</c> コマンド。アタッチ済みウィンドウを最大化し、成功時に snapshot を自動取得する。
/// </summary>
internal static class MaximizeCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>maximize サブコマンド。</returns>
    public static Command Build()
        => WindowStateCommandBuilder.Build(
            name: "maximize",
            description: "Maximize the attached window via UIA WindowPattern.SetWindowVisualState(Maximized).",
            toolName: "windows_maximize");
}
