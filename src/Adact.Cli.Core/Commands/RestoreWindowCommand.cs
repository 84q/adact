using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>restore-window</c> コマンド。最小化/最大化済みウィンドウを通常表示へ復元し、成功時に snapshot を自動取得する。
/// </summary>
internal static class RestoreWindowCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>restore-window サブコマンド。</returns>
    public static Command Build()
        => WindowStateCommandBuilder.Build(
            name: "restore-window",
            description: "Restore the attached window to normal state via UIA WindowPattern.SetWindowVisualState(Normal).",
            toolName: "adact_restore_window");
}
