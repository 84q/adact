using System.CommandLine;

using Adact.Cli.Commands;

namespace Adact.Cli;

/// <summary>
/// Entry point for the adact CLI. Wires up the System.CommandLine root command.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Parses the command line and invokes the root command returned by <see cref="BuildRoot"/>.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Process exit code (see docs/spec/errors-and-output.md).</returns>
    public static async Task<int> Main(string[] args)
    {
        using var _ = CommandHelpers.PushRuntime(
            CommandHelpers.CommandRuntime.CreateDefault(Daemon.DaemonSpawner.EnsureServerRunningAsync));

        var root = BuildRoot();
        var parseResult = root.Parse(args);
        return await parseResult.InvokeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the root command. Used by unit tests and internal call sites.
    /// </summary>
    internal static RootCommand BuildRoot()
    {
        var root = RootCommandRegistration.CreateRoot("ADACT - AI-driven Desktop Application CLI Tools");

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
