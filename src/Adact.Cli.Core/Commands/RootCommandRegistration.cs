using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class RootCommandRegistration
{
    /// <summary>
    /// </summary>
    public static RootCommand CreateRoot(string description)
    {
        var root = new RootCommand(description);
        root.Options.Add(CommandHelpers.ServerOption);
        return root;
    }

    /// <summary>
    /// </summary>
    public static void AddSharedCommands(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);

        command.Subcommands.Add(ListWindowsCommand.Build());
        command.Subcommands.Add(AttachCommand.Build());
        command.Subcommands.Add(SnapshotCommand.Build());
        command.Subcommands.Add(ClickCommand.Build());
        command.Subcommands.Add(FillCommand.Build());
        command.Subcommands.Add(DoubleclickCommand.Build());
        command.Subcommands.Add(HoverCommand.Build());
        command.Subcommands.Add(MousemoveCommand.Build());
        command.Subcommands.Add(MousedownCommand.Build());
        command.Subcommands.Add(MouseupCommand.Build());
        command.Subcommands.Add(MousewheelCommand.Build());
        command.Subcommands.Add(KeypressCommand.Build());
        command.Subcommands.Add(KeydownCommand.Build());
        command.Subcommands.Add(KeyupCommand.Build());
        command.Subcommands.Add(TypeCommand.Build());
        command.Subcommands.Add(CheckCommand.Build());
        command.Subcommands.Add(UncheckCommand.Build());
        command.Subcommands.Add(SelectCommand.Build());
        command.Subcommands.Add(FocusCommand.Build());
        command.Subcommands.Add(ScrollIntoViewCommand.Build());
        command.Subcommands.Add(ScrollCommand.Build());
        command.Subcommands.Add(ResizeWindowCommand.Build());
        command.Subcommands.Add(MinimizeWindowCommand.Build());
        command.Subcommands.Add(MaximizeWindowCommand.Build());
        command.Subcommands.Add(RestoreWindowCommand.Build());
        command.Subcommands.Add(InspectCommand.Build());
        command.Subcommands.Add(ScreenshotCommand.Build());
        command.Subcommands.Add(WaitForElementCommand.Build());
        command.Subcommands.Add(WaitForWindowCommand.Build());
        command.Subcommands.Add(DetachCommand.Build());
        command.Subcommands.Add(CloseWindowCommand.Build());
        command.Subcommands.Add(KillCommand.Build());
    }

    /// <summary>
    /// </summary>
    public static void AddInstallAndLaunchCommands(Command command, bool launchBeforeInstall)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (launchBeforeInstall)
        {
            command.Subcommands.Add(LaunchCommand.Build());
            command.Subcommands.Add(InstallCommand.Build());
            return;
        }

        command.Subcommands.Add(InstallCommand.Build());
        command.Subcommands.Add(LaunchCommand.Build());
    }
}
