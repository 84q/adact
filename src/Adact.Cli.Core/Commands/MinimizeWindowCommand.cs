using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class MinimizeWindowCommand
{
    public static Command Build()
        => WindowStateCommandBuilder.Build(
            name: "minimize-window",
            description: "Minimize the attached window via UIA WindowPattern.SetWindowVisualState(Minimized).",
            toolName: "adact_minimize_window");
}
