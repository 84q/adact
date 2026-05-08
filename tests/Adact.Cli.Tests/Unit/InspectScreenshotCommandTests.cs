using System.CommandLine;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Inspect Screenshot Command behavior.</summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class InspectScreenshotCommandTests
{
    /// <summary>Performs the Inspect Malformed Ref Returns User Error operation.</summary>
    [Fact]
    public async Task Inspect_MalformedRef_ReturnsUserError()
    {
        var (stdout, stderr, exit) = await RunInspectAsync(["inspect", "not-a-ref"]);

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: " + ErrorCodes.InvalidRefFormat, stdout, StringComparison.Ordinal);
    }

    /// <summary>Performs the Inspect Missing Ref Returns User Error operation.</summary>
    [Fact]
    public async Task Inspect_MissingRef_ReturnsUserError()
    {
        var (_, _, exit) = await RunInspectAsync(["inspect"]);
        Assert.NotEqual(ExitCodes.Success, exit);
    }

    /// <summary>Performs the Screenshot Non Ref Target Is Accepted As Session Id operation.</summary>
    [Fact]
    public async Task Screenshot_NonRefTarget_IsAcceptedAsSessionId()
    {
        var (_, stderr, exit) = await RunScreenshotAsync(["screenshot", "bad"]);
        Assert.Equal(string.Empty, stderr);
        Assert.NotEqual(ExitCodes.UserError, exit);
    }

    /// <summary>Performs the Inspect Has Required Ref Argument operation.</summary>
    [Fact]
    public void Inspect_HasRequiredRefArgument()
    {
        var cmd = InspectCommand.Build();
        Assert.Equal("inspect", cmd.Name);
        var refArg = cmd.Arguments.FirstOrDefault(a => a.Name == "ref");
        Assert.NotNull(refArg);
    }

    /// <summary>Performs the Screenshot Has Optional Ref And Out operation.</summary>
    [Fact]
    public void Screenshot_HasOptionalRefAndOut()
    {
        var cmd = ScreenshotCommand.Build();
        Assert.Equal("screenshot", cmd.Name);
        var targetArg = cmd.Arguments.FirstOrDefault(a => a.Name == "target");
        var outOpt = cmd.Options.FirstOrDefault(o => o.Name == "--out");
        Assert.NotNull(targetArg);
        Assert.NotNull(outOpt);
        Assert.Equal(ArgumentArity.ZeroOrOne, targetArg!.Arity);
        Assert.False(outOpt!.Required);
    }

    private static Task<(string stdout, string stderr, int exit)> RunInspectAsync(string[] args)
        => RunAsync(args, root => root.Subcommands.Add(InspectCommand.Build()));

    private static Task<(string stdout, string stderr, int exit)> RunScreenshotAsync(string[] args)
        => RunAsync(args, root => root.Subcommands.Add(ScreenshotCommand.Build()));

    private static async Task<(string stdout, string stderr, int exit)> RunAsync(string[] args, Action<RootCommand> configure)
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
            configure(root);
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
