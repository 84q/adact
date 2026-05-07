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
        var root = RootCommandRegistration.CreateRoot("ADACT - AI-driven Desktop Application CLI Tools");

        // serve サブコマンド（http / pipe）
        var serveCmd = new Command("serve", "Run as an MCP server (http or pipe transport).");
        serveCmd.Subcommands.Add(ServeHttpCommand.Build());
        serveCmd.Subcommands.Add(ServePipeCommand.Build());
        root.Subcommands.Add(serveCmd);
        RootCommandRegistration.AddSharedCommands(root);
        root.Subcommands.Add(DaemonStopCommand.Build());
        RootCommandRegistration.AddInstallAndLaunchCommands(root, launchBeforeInstall: false);
        return root;
    }
}
