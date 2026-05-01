using System.CommandLine;
using Adact.Cli.Commands;

namespace Adact.Cli.Client;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var root = new RootCommand("ADACT - Cross-platform CLI Client");
        // Cross-platform: exclude local/serve/daemon-stop/install
        root.Subcommands.Add(ListAppsCommand.Build());
        root.Subcommands.Add(AttachCommand.Build());
        root.Subcommands.Add(SnapshotCommand.Build());
        root.Subcommands.Add(ClickCommand.Build());
        root.Subcommands.Add(FillCommand.Build());
        root.Subcommands.Add(DblclickCommand.Build());
        root.Subcommands.Add(HoverCommand.Build());
        root.Subcommands.Add(MouseMoveCommand.Build());
        root.Subcommands.Add(MouseDownCommand.Build());
        root.Subcommands.Add(MouseUpCommand.Build());
        root.Subcommands.Add(MouseWheelCommand.Build());
        root.Subcommands.Add(PressCommand.Build());
        root.Subcommands.Add(KeyDownCommand.Build());
        root.Subcommands.Add(KeyUpCommand.Build());
        root.Subcommands.Add(TypeCommand.Build());
        root.Subcommands.Add(CheckCommand.Build());
        root.Subcommands.Add(UncheckCommand.Build());
        root.Subcommands.Add(SelectCommand.Build());
        root.Subcommands.Add(FocusCommand.Build());
        root.Subcommands.Add(ClearCommand.Build());
        root.Subcommands.Add(ScrollIntoViewCommand.Build());
        root.Subcommands.Add(ResizeCommand.Build());
        root.Subcommands.Add(MinimizeCommand.Build());
        root.Subcommands.Add(MaximizeCommand.Build());
        root.Subcommands.Add(RestoreCommand.Build());
        root.Subcommands.Add(InspectCommand.Build());
        root.Subcommands.Add(ScreenshotCommand.Build());
        root.Subcommands.Add(WaitForCommand.Build());
        root.Subcommands.Add(WaitForWindowCommand.Build());
        root.Subcommands.Add(DetachCommand.Build());
        root.Subcommands.Add(CloseCommand.Build());
        root.Subcommands.Add(KillCommand.Build());
        root.Subcommands.Add(CloseAllCommand.Build());
        root.Subcommands.Add(LaunchCommand.Build());
        return await root.Parse(args).InvokeAsync();
    }
}
