using System.CommandLine;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Daemon Stop Command behavior.</summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class DaemonStopCommandTests
{
    /// <summary>Performs the Daemon Stop Non Localhost Returns Local Only And Exit2 operation.</summary>
    [Theory]
    [InlineData("http://192.168.1.10:41300/mcp")]
    [InlineData("https://example.com/mcp")]
    public async Task DaemonStop_NonLocalhost_ReturnsLocalOnlyAndExit2(string remote)
    {
        var (stdout, stderr, exit) = await RunAsync(["daemon-stop", "--server", remote]);

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: " + ErrorCodes.LocalOnly, stdout, StringComparison.Ordinal);
    }

    /// <summary>Performs the Daemon Stop With Server Returns Local Only operation.</summary>
    [Fact]
    public async Task DaemonStop_WithServer_ReturnsLocalOnly()
    {
        var (stdout, stderr, exit) = await RunAsync(["daemon-stop", "--server", "http://127.0.0.1:41300/mcp"]);

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: " + ErrorCodes.LocalOnly, stdout, StringComparison.Ordinal);
        Assert.Contains("not supported for HTTP mode", stdout, StringComparison.Ordinal);
    }

    /// <summary>Gets a value indicating whether Is Connection Drop Exception Connection Exceptions Return True.</summary>
    [Theory]
    [MemberData(nameof(ConnectionDropCases))]
    public void IsConnectionDropException_ConnectionExceptions_ReturnTrue(Exception ex)
    {
        Assert.True(DaemonStopCommand.IsConnectionDropException(ex));
    }

    /// <summary>Gets a value indicating whether Is Connection Drop Exception Other Exceptions Return False.</summary>
    [Theory]
    [MemberData(nameof(NonConnectionDropCases))]
    public void IsConnectionDropException_OtherExceptions_ReturnFalse(Exception ex)
    {
        Assert.False(DaemonStopCommand.IsConnectionDropException(ex));
    }

    /// <summary>Performs the Connection Drop Cases operation.</summary>
    public static IEnumerable<object[]> ConnectionDropCases()
    {
        yield return new object[] { new IOException("io") };
        yield return new object[] { new ObjectDisposedException("x") };
        yield return new object[] { new InvalidOperationException("wrap", new IOException("inner")) };
    }

    /// <summary>Performs the Non Connection Drop Cases operation.</summary>
    public static IEnumerable<object[]> NonConnectionDropCases()
    {
        yield return new object[] { new OperationCanceledException() };
        yield return new object[] { new TaskCanceledException() };
        yield return new object[] { new InvalidOperationException("plain") };
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

            var root = new RootCommand("test");
            root.Options.Add(CommandHelpers.ServerOption);
            root.Subcommands.Add(DaemonStopCommand.Build());
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
