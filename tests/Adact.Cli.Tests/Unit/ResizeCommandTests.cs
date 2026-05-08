using System.CommandLine;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Resize Command behavior.</summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class ResizeCommandTests
{
    /// <summary>Performs the Resize Non Positive Width Returns User Error operation.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Resize_NonPositiveWidth_ReturnsUserError(string width)
    {
        var (stdout, stderr, exit) = await RunAsync(["resize-window", "--width", width, "--height", "100"]);

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: " + ErrorCodes.InvalidArgument, stdout, StringComparison.Ordinal);
        Assert.Contains("--width", stdout, StringComparison.Ordinal);
    }

    /// <summary>Performs the Resize Non Positive Height Returns User Error operation.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Resize_NonPositiveHeight_ReturnsUserError(string height)
    {
        var (stdout, stderr, exit) = await RunAsync(["resize-window", "--width", "100", "--height", height]);

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: " + ErrorCodes.InvalidArgument, stdout, StringComparison.Ordinal);
        Assert.Contains("--height", stdout, StringComparison.Ordinal);
    }

    /// <summary>Performs the Resize Missing Width Returns User Error operation.</summary>
    [Fact]
    public async Task Resize_MissingWidth_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(["resize-window", "--height", "100"]);

        Assert.NotEqual(ExitCodes.Success, exit);
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
            root.Subcommands.Add(ResizeWindowCommand.Build());
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
