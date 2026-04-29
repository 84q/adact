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
        root.Subcommands.Add(LocalCommand.Build());
        root.Subcommands.Add(ServeCommand.Build());
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
        root.Subcommands.Add(DetachCommand.Build());
        root.Subcommands.Add(CloseCommand.Build());
        root.Subcommands.Add(KillCommand.Build());
        root.Subcommands.Add(CloseAllCommand.Build());
        root.Subcommands.Add(DaemonStopCommand.Build());
        root.Subcommands.Add(InstallCommand.Build());
        root.Subcommands.Add(LaunchCommand.Build());
        return root;
    }
}
