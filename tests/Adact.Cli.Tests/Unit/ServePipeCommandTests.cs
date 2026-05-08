using System.CommandLine;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Serve Pipe Command behavior.</summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class ServePipeCommandTests
{
    /// <summary>Performs the Serve Pipe Pipe Already Exists Returns Already Running Error operation.</summary>
    [Fact]
    public async Task ServePipe_PipeAlreadyExists_ReturnsAlreadyRunningError()
    {
        var origCheck = ServePipeCommand.IsServerRunningAsync;
        try
        {
            ServePipeCommand.IsServerRunningAsync = static (_, _, _) => Task.FromResult(true);
            var (stdout, stderr, exit) = await RunAsync(["serve", "pipe"]);

            Assert.Equal(ExitCodes.CommandFailed, exit);
            Assert.Contains("error: " + ErrorCodes.AlreadyRunning, stdout, StringComparison.Ordinal);
        }
        finally
        {
            ServePipeCommand.IsServerRunningAsync = origCheck;
        }
    }

    /// <summary>Performs the Serve Pipe No Existing Pipe Starts Server operation.</summary>
    [Fact]
    public async Task ServePipe_NoExistingPipe_StartsServer()
    {
        var origCheck = ServePipeCommand.IsServerRunningAsync;
        var origRun = ServePipeCommand.RunNamedPipeHostAsync;
        try
        {
            ServePipeCommand.IsServerRunningAsync = static (_, _, _) => Task.FromResult(false);
            ServePipeCommand.RunNamedPipeHostAsync = static (_, _) => Task.FromResult(0);
            var (_, stderr, exit) = await RunAsync(["serve", "pipe"]);

            Assert.Equal(ExitCodes.Success, exit);
            Assert.Empty(stderr);
        }
        finally
        {
            ServePipeCommand.IsServerRunningAsync = origCheck;
            ServePipeCommand.RunNamedPipeHostAsync = origRun;
        }
    }

    private static async Task<(string stdout, string stderr, int exit)> RunAsync(string[] args)
    {
        var origOut = Console.Out;
        var origErr = Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        try
        {
            Console.SetOut(outWriter);
            Console.SetError(errWriter);

            var root = Program.BuildRoot();
            var parse = root.Parse(args);
            var exit = await parse.InvokeAsync().ConfigureAwait(false);
            return (outWriter.ToString(), errWriter.ToString(), exit);
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }
}
