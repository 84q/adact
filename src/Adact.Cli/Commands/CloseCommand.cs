using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class CloseCommand
{
  public static Command Build()
  {
    var sid = new Option<string?>("--sid") { Description = "Target session ID (default: active session)." };
    var server = CommandHelpers.CreateServerOption();

    var cmd = new Command("close", "Close a window via UIA WindowPattern.Close (auto-detach on success).");
    cmd.Options.Add(sid);
    cmd.Options.Add(server);

    cmd.SetAction((parseResult, ct) =>
    {
      var sidArg = parseResult.GetValue(sid);
      var serverArg = parseResult.GetValue(server);

      return CommandHelpers.RunWithClientAsync(
              serverArg,
              (client, token) => LifecycleCommandImpl.ExecuteAsync(
                  client, "windows_close", sidArg, ["closed", "detached"], token),
              ct);
    });

    return cmd;
  }
}
