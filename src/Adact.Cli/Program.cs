using System.CommandLine;

using Adact.Cli.Commands;

namespace Adact.Cli;

internal static class Program
{
  public static async Task<int> Main(string[] args)
  {
    var root = new RootCommand("ADACT - AI-driven Desktop Application CLI Tools");
    root.Subcommands.Add(LocalCommand.Build());
    root.Subcommands.Add(ServeCommand.Build());
    root.Subcommands.Add(ListAppsCommand.Build());
    root.Subcommands.Add(AttachCommand.Build());
    root.Subcommands.Add(SnapshotCommand.Build());
    root.Subcommands.Add(ClickCommand.Build());
    root.Subcommands.Add(FillCommand.Build());
    root.Subcommands.Add(DetachCommand.Build());
    root.Subcommands.Add(CloseCommand.Build());
    root.Subcommands.Add(KillCommand.Build());
    root.Subcommands.Add(CloseAllCommand.Build());
    root.Subcommands.Add(DaemonStopCommand.Build());

    var parseResult = root.Parse(args);
    return await parseResult.InvokeAsync().ConfigureAwait(false);
  }
}
