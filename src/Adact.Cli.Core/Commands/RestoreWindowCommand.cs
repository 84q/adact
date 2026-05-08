using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class RestoreWindowCommand
{
    public static Command Build()
        => WindowStateCommandBuilder.Build(
            name: "restore-window",
            description: "Restore the attached window to normal state via UIA WindowPattern.SetWindowVisualState(Normal).",
            toolName: "adact_restore_window");
}
