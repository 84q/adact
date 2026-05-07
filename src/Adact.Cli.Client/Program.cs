using System.CommandLine;

using Adact.Cli.Commands;

namespace Adact.Cli.Client;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var root = BuildRoot();
        return await root.Parse(args).InvokeAsync();
    }

    internal static RootCommand BuildRoot()
    {
        var root = RootCommandRegistration.CreateRoot("ADACT - Cross-platform CLI Client");
        // Cross-platform: exclude local/serve/daemon-stop
        RootCommandRegistration.AddSharedCommands(root);
        RootCommandRegistration.AddInstallAndLaunchCommands(root, launchBeforeInstall: true);
        return root;
    }
}
