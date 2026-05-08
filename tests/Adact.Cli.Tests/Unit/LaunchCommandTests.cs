using System.CommandLine;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Launch Command behavior.</summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class LaunchCommandTests
{
    /// <summary>Attempts to perform the Try Parse Env Multiple Entries Parses All operation.</summary>
    [Fact]
    public void TryParseEnv_MultipleEntries_ParsesAll()
    {
        var ok = LaunchCommand.TryParseEnv(
            ["FOO=bar", "BAZ=qux=quux", "EMPTY="],
            out var dict, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("bar", dict["FOO"]);
        Assert.Equal("qux=quux", dict["BAZ"]);
        Assert.Equal("", dict["EMPTY"]);
    }

    /// <summary>Attempts to perform the Try Parse Env Invalid Entry Returns Error operation.</summary>
    [Theory]
    [InlineData("FOO")]
    [InlineData("=BAR")]
    public void TryParseEnv_InvalidEntry_ReturnsError(string entry)
    {
        var ok = LaunchCommand.TryParseEnv(
            [entry], out var dict, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Empty(dict);
    }

    /// <summary>Attempts to perform the Try Parse Env Empty Entry Returns Error operation.</summary>
    [Fact]
    public void TryParseEnv_EmptyEntry_ReturnsError()
    {
        var ok = LaunchCommand.TryParseEnv(
            [""], out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    /// <summary>Performs the Launch Invalid Env Returns User Error operation.</summary>
    [Fact]
    public async Task Launch_InvalidEnv_ReturnsUserError()
    {
        var (stdout, stderr, exit) = await RunAsync(["launch", "notepad.exe", "--env", "INVALID"]);

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: " + ErrorCodes.InvalidArgument, stdout, StringComparison.Ordinal);
    }

    /// <summary>Performs the Launch Missing Executable Returns User Error operation.</summary>
    [Fact]
    public async Task Launch_MissingExecutable_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(["launch"]);

        Assert.NotEqual(ExitCodes.Success, exit);
    }


    /// <summary>Performs the Parse Raw Arguments After Double Dash Are Passed Through operation.</summary>
    [Fact]
    public void Parse_RawArgumentsAfterDoubleDash_ArePassedThrough()
    {
        var (parse, argsArg, _, _) = BuildAndParse(
            ["launch", "notepad.exe", "--", "arg1", "arg2"]);

        var values = parse.GetValue(argsArg);
        Assert.NotNull(values);
        Assert.Equal(new[] { "arg1", "arg2" }, values);
    }

    /// <summary>Performs the Parse Raw Arguments After Double Dash Preserves Spaces And Option Like Tokens operation.</summary>
    [Fact]
    public void Parse_RawArgumentsAfterDoubleDash_PreservesSpacesAndOptionLikeTokens()
    {
        var (parse, argsArg, _, _) = BuildAndParse(
            ["launch", "notepad.exe", "--", "with space", "--flag", "-x"]);

        var values = parse.GetValue(argsArg);
        Assert.NotNull(values);
        Assert.Equal(new[] { "with space", "--flag", "-x" }, values);
    }

    /// <summary>Performs the Parse Cwd Option Passes Path Through operation.</summary>
    [Fact]
    public void Parse_CwdOption_PassesPathThrough()
    {
        var (parse, _, cwdOpt, _) = BuildAndParse(
            ["launch", "notepad.exe", "--cwd", "C:\\some\\path"]);

        var cwd = parse.GetValue(cwdOpt);
        Assert.Equal("C:\\some\\path", cwd);
    }

    /// <summary>Performs the Parse Env Option With Multiple Equals Signs Keeps Value Intact operation.</summary>
    [Fact]
    public void Parse_EnvOption_WithMultipleEqualsSigns_KeepsValueIntact()
    {
        var (parse, _, _, envOpt) = BuildAndParse(
            ["launch", "notepad.exe",
             "--env", "FOO=bar",
             "--env", "BAZ=qux=quux"]);

        var entries = parse.GetValue(envOpt);
        Assert.NotNull(entries);
        Assert.Equal(new[] { "FOO=bar", "BAZ=qux=quux" }, entries);

        Assert.True(LaunchCommand.TryParseEnv(entries!, out var dict, out var error));
        Assert.Null(error);
        Assert.Equal("bar", dict["FOO"]);
        Assert.Equal("qux=quux", dict["BAZ"]);
    }

    private static (
        System.CommandLine.ParseResult parse,
        Argument<string[]> argsArg,
        Option<string?> cwdOpt,
        Option<string[]> envOpt)
        BuildAndParse(string[] argv)
    {
        var launch = LaunchCommand.Build();
        var argsArg = (Argument<string[]>)launch.Arguments.First(a => a.Name == "args");
        var cwdOpt = (Option<string?>)launch.Options.First(o => o.Name == "--cwd");
        var envOpt = (Option<string[]>)launch.Options.First(o => o.Name == "--env");

        var root = new RootCommand("test");
        root.Subcommands.Add(launch);
        var parse = root.Parse(argv);
        return (parse, argsArg, cwdOpt, envOpt);
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
            root.Subcommands.Add(LaunchCommand.Build());
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
