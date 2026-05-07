using System.CommandLine;

namespace Adact.Cli.Commands;

/// <summary>
/// <see cref="RootCommand"/> の共通初期化と、CLI / CLI Client 間で共有するサブコマンド登録をまとめる。
/// </summary>
internal static class RootCommandRegistration
{
    /// <summary>
    /// 説明文と共通 <c>--server</c> Option を持つ <see cref="RootCommand"/> を生成する。
    /// </summary>
    /// <param name="description">ルートコマンド説明文。</param>
    /// <returns>初期化済みの <see cref="RootCommand"/>。</returns>
    public static RootCommand CreateRoot(string description)
    {
        var root = new RootCommand(description);
        root.Options.Add(CommandHelpers.ServerOption);
        return root;
    }

    /// <summary>
    /// <c>serve</c> / <c>daemon-stop</c> を除く共通サブコマンドを登録する。
    /// <c>install</c> / <c>launch</c> は順序差分を維持するため別メソッドで登録する。
    /// </summary>
    /// <param name="command">登録先のコマンド。</param>
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
        command.Subcommands.Add(ClearCommand.Build());
        command.Subcommands.Add(ScrollIntoViewCommand.Build());
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
        command.Subcommands.Add(CloseAllCommand.Build());
    }

    /// <summary>
    /// 両 CLI で共通な <c>install</c> / <c>launch</c> を、呼び出し側が必要とする順序で登録する。
    /// </summary>
    /// <param name="command">登録先のコマンド。</param>
    /// <param name="launchBeforeInstall"><c>true</c> の場合は launch → install、そうでなければ install → launch。</param>
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
