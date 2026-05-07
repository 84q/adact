using System.CommandLine;

using Adact.Cli.Commands;

namespace Adact.Cli;

/// <summary>
/// adact CLI のエントリポイント。サブコマンドをルートに集約して System.CommandLine に委譲する。
/// </summary>
internal static class Program
{
    /// <summary>
    /// CLI の Main エントリ。引数を parse し <see cref="BuildRoot"/> で生成したルートコマンドを実行する。
    /// </summary>
    /// <param name="args">コマンドライン引数。</param>
    /// <returns>サブコマンドが返した exit code (設計 docs/spec/errors-and-output.md)。</returns>
    public static async Task<int> Main(string[] args)
    {
        using var _ = CommandHelpers.PushRuntime(
            CommandHelpers.CommandRuntime.CreateDefault(Daemon.DaemonSpawner.EnsureServerRunningAsync));

        var root = BuildRoot();
        var parseResult = root.Parse(args);
        return await parseResult.InvokeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// ルートコマンドを生成する。Unit テスト (例: Skill references とサブコマンド名の整合検証)
    /// から呼び出すため internal で公開する。
    /// </summary>
    internal static RootCommand BuildRoot()
    {
        var root = new RootCommand("ADACT - AI-driven Desktop Application CLI Tools");
        root.Options.Add(CommandHelpers.ServerOption);

        // serve サブコマンド（http / pipe）
        var serveCmd = new Command("serve", "Run as an MCP server (http or pipe transport).");
        serveCmd.Subcommands.Add(ServeHttpCommand.Build());
        serveCmd.Subcommands.Add(ServePipeCommand.Build());
        root.Subcommands.Add(serveCmd);
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
        root.Subcommands.Add(DaemonStopCommand.Build());
        root.Subcommands.Add(InstallCommand.Build());
        root.Subcommands.Add(LaunchCommand.Build());
        return root;
    }
}
