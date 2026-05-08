using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class MaximizeWindowCommand
{
    public static Command Build()
        => WindowStateCommandBuilder.Build(
            name: "maximize-window",
            description: "Maximize the attached window via UIA WindowPattern.SetWindowVisualState(Maximized).",
            toolName: "adact_maximize_window");
}
