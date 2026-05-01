using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// <c>restore</c> コマンド。最小化/最大化済みウィンドウを通常表示へ復元し、成功時に snapshot を自動取得する。
/// </summary>
internal static class RestoreCommand
{
    /// <summary>System.CommandLine 用の <see cref="Command"/> を生成する。</summary>
    /// <returns>restore サブコマンド。</returns>
    public static Command Build()
        => WindowStateCommandBuilder.Build(
            name: "restore",
            description: "Restore the attached window to normal state via UIA WindowPattern.SetWindowVisualState(Normal).",
            toolName: "windows_restore");
}
