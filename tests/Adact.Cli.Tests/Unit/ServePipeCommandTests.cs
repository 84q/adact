using System.CommandLine;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="ServePipeCommand"/> の Unit テスト。
/// パイプ存在確認・サーバー起動の分岐を検証する。
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class ServePipeCommandTests
{
    /// <summary>
    /// パイプが既に存在する場合、ALREADY_RUNNING エラーと exit=1 (CommandFailed) となることを確認する。
    /// </summary>
    [Fact]
    public async Task ServePipe_PipeAlreadyExists_ReturnsAlreadyRunningError()
    {
        var origCheck = ServePipeCommand.IsServerRunningAsync;
        try
        {
            ServePipeCommand.IsServerRunningAsync = static (_, _, _) => Task.FromResult(true);
            var (_, stderr, exit) = await RunAsync(["serve", "pipe"]);

            Assert.Equal(ExitCodes.CommandFailed, exit);
            Assert.Contains("error " + ErrorCodes.AlreadyRunning, stderr, StringComparison.Ordinal);
        }
        finally
        {
            ServePipeCommand.IsServerRunningAsync = origCheck;
        }
    }

    /// <summary>
    /// パイプがない場合、サーバーが起動し、正常終了 (exit 0) となることを確認する。
    /// </summary>
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
