using System.CommandLine;

using Adact.Cli.Commands;

namespace Adact.Cli.Client;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var root = new RootCommand("ADACT - Cross-platform CLI Client");
        root.Options.Add(CommandHelpers.ServerOption);
        // Cross-platform: exclude local/serve/daemon-stop
        root.Subcommands.Add(ListWindowsCommand.Build());
        root.Subcommands.Add(AttachCommand.Build());
        root.Subcommands.Add(SnapshotCommand.Build());
        root.Subcommands.Add(ClickCommand.Build());
        root.Subcommands.Add(FillCommand.Build());
        root.Subcommands.Add(DoubleclickCommand.Build());
        root.Subcommands.Add(HoverCommand.Build());
        root.Subcommands.Add(MousemoveCommand.Build());
        root.Subcommands.Add(MousedownCommand.Build());
        root.Subcommands.Add(MouseupCommand.Build());
        root.Subcommands.Add(MousewheelCommand.Build());
        root.Subcommands.Add(KeypressCommand.Build());
        root.Subcommands.Add(KeydownCommand.Build());
        root.Subcommands.Add(KeyupCommand.Build());
        root.Subcommands.Add(TypeCommand.Build());
        root.Subcommands.Add(CheckCommand.Build());
        root.Subcommands.Add(UncheckCommand.Build());
        root.Subcommands.Add(SelectCommand.Build());
        root.Subcommands.Add(FocusCommand.Build());
        root.Subcommands.Add(ScrollIntoViewCommand.Build());
        root.Subcommands.Add(ScrollCommand.Build());
        root.Subcommands.Add(ResizeWindowCommand.Build());
        root.Subcommands.Add(MinimizeWindowCommand.Build());
        root.Subcommands.Add(MaximizeWindowCommand.Build());
        root.Subcommands.Add(RestoreWindowCommand.Build());
        root.Subcommands.Add(InspectCommand.Build());
        root.Subcommands.Add(ScreenshotCommand.Build());
        root.Subcommands.Add(WaitForElementCommand.Build());
        root.Subcommands.Add(WaitForWindowCommand.Build());
        root.Subcommands.Add(DetachCommand.Build());
        root.Subcommands.Add(CloseWindowCommand.Build());
        root.Subcommands.Add(KillCommand.Build());
        root.Subcommands.Add(LaunchCommand.Build());
        root.Subcommands.Add(InstallCommand.Build());
        return await root.Parse(args).InvokeAsync();
    }
}
