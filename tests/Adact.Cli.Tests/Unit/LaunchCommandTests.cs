using System.CommandLine;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="LaunchCommand"/> の引数パース / 検証 (設計 024 §7) を検証する Unit テスト。
/// 接続前 (parser / SetAction 段階) で弾かれるケースのみを対象とし、実 daemon / プロセス起動は行わない。
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class LaunchCommandTests
{
    /// <summary>--env KEY=VALUE が複数指定とパースされ、値側に <c>=</c> を含めても全体が値として保持される。</summary>
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

    /// <summary><c>=</c> を含まない --env は UserError 相当のパースエラーになる。</summary>
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

    /// <summary>空エントリは UserError 相当。</summary>
    [Fact]
    public void TryParseEnv_EmptyEntry_ReturnsError()
    {
        var ok = LaunchCommand.TryParseEnv(
            [""], out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    /// <summary>不正な --env (= なし) を CLI 経由で渡すと UserError exit と INVALID_ARGUMENT が返る。</summary>
    [Fact]
    public async Task Launch_InvalidEnv_ReturnsUserError()
    {
        var (stdout, stderr, exit) = await RunAsync(["launch", "notepad.exe", "--env", "INVALID"]);

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("error: " + ErrorCodes.InvalidArgument, stdout, StringComparison.Ordinal);
    }

    /// <summary>executable 引数を省略すると System.CommandLine が UserError を返す。</summary>
    [Fact]
    public async Task Launch_MissingExecutable_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(["launch"]);

        Assert.NotEqual(ExitCodes.Success, exit);
    }

    // ---- parser-only tests (SetAction を起動しない) ----
    // 設計 024 §7 G1 / S3: --, --cwd, --env の透過を直接 ParseResult から検証する。

    /// <summary><c>-- arg1 arg2</c> 以降の raw 引数が <c>Argument&lt;string[]&gt;("args")</c>
    /// にそのまま流れ込むことを確認する。</summary>
    [Fact]
    public void Parse_RawArgumentsAfterDoubleDash_ArePassedThrough()
    {
        var (parse, argsArg, _, _) = BuildAndParse(
            ["launch", "notepad.exe", "--", "arg1", "arg2"]);

        var values = parse.GetValue(argsArg);
        Assert.NotNull(values);
        Assert.Equal(new[] { "arg1", "arg2" }, values);
    }

    /// <summary><c>-- "with space" --flag</c> のように空白入り / オプション風トークンも
    /// 解釈されず raw のまま透過する。</summary>
    [Fact]
    public void Parse_RawArgumentsAfterDoubleDash_PreservesSpacesAndOptionLikeTokens()
    {
        var (parse, argsArg, _, _) = BuildAndParse(
            ["launch", "notepad.exe", "--", "with space", "--flag", "-x"]);

        var values = parse.GetValue(argsArg);
        Assert.NotNull(values);
        Assert.Equal(new[] { "with space", "--flag", "-x" }, values);
    }

    /// <summary><c>--cwd &lt;path&gt;</c> がそのまま <see cref="string"/> 値として取り出せる。</summary>
    [Fact]
    public void Parse_CwdOption_PassesPathThrough()
    {
        var (parse, _, cwdOpt, _) = BuildAndParse(
            ["launch", "notepad.exe", "--cwd", "C:\\some\\path"]);

        var cwd = parse.GetValue(cwdOpt);
        Assert.Equal("C:\\some\\path", cwd);
    }

    /// <summary><c>--env "BAZ=qux=quux"</c> のように <c>=</c> を 2 つ含む値が parser を通っても
    /// 1 トークンとして保持され、<see cref="LaunchCommand.TryParseEnv"/> で正しく分割される。</summary>
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
