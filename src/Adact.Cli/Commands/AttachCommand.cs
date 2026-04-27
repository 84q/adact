using System.CommandLine;

namespace Adact.Cli.Commands;

internal static class AttachCommand
{
    public static Command Build()
    {
        var refArg = new Argument<string?>("ref")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Window Ref ID like 'w1' (from list-apps).",
        };
        var processName = new Option<string?>("--process-name") { Description = "Process name (e.g. calc.exe)." };
        var title = new Option<string?>("--title") { Description = "Window title substring." };
        var processId = new Option<int?>("--process-id") { Description = "Process ID." };
        var className = new Option<string?>("--class-name") { Description = "Win32 class name." };
        var noSnapshot = new Option<bool>("--no-snapshot") { Description = "Do not capture a snapshot on success." };
        var snapshotDir = new Option<string?>("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };
        var server = new Option<string?>("--server") { Description = "Connection target URL." };

        var cmd = new Command("attach", "Attach to a window as a session.");
        cmd.Arguments.Add(refArg);
        cmd.Options.Add(processName);
        cmd.Options.Add(title);
        cmd.Options.Add(processId);
        cmd.Options.Add(className);
        cmd.Options.Add(noSnapshot);
        cmd.Options.Add(snapshotDir);
        cmd.Options.Add(server);

        cmd.SetAction(parseResult =>
        {
            _ = parseResult.GetValue(refArg);
            _ = parseResult.GetValue(processName);
            _ = parseResult.GetValue(title);
            _ = parseResult.GetValue(processId);
            _ = parseResult.GetValue(className);
            _ = parseResult.GetValue(noSnapshot);
            _ = parseResult.GetValue(snapshotDir);
            _ = parseResult.GetValue(server);
            return CommandHelpers.NotYetImplemented("attach");
        });

        return cmd;
    }
}
