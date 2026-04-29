using System.CommandLine;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="WaitForCommand"/> および <see cref="WaitForWindowCommand"/> (Phase 8 Step 7) の
/// 引数パース・ローカルバリデーション検証。daemon / UIA への接続は行わない。
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class WaitForCommandTests
{
    /// <summary>wait-for: --ref と検索条件の同時指定は INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitFor_RefAndQuery_ReturnsUserError()
    {
        var (_, stderr, exit) = await RunWaitForAsync(["wait-for", "--ref", "s1e1", "--name", "OK"]);
        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Contains("error " + ErrorCodes.InvalidArgument, stderr, StringComparison.Ordinal);
    }

    /// <summary>wait-for: ref も検索条件も無いと INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitFor_NoConditions_ReturnsUserError()
    {
        var (_, stderr, exit) = await RunWaitForAsync(["wait-for"]);
        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Contains("error " + ErrorCodes.InvalidArgument, stderr, StringComparison.Ordinal);
    }

    /// <summary>wait-for: 形式不正な --ref は INVALID_REF_FORMAT。</summary>
    [Fact]
    public async Task WaitFor_MalformedRef_ReturnsUserError()
    {
        var (_, stderr, exit) = await RunWaitForAsync(["wait-for", "--ref", "not-a-ref"]);
        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Contains("error " + ErrorCodes.InvalidRefFormat, stderr, StringComparison.Ordinal);
    }

    /// <summary>wait-for: 未知の --state は INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitFor_UnknownState_ReturnsUserError()
    {
        var (_, stderr, exit) = await RunWaitForAsync(["wait-for", "--ref", "s1e1", "--state", "focused"]);
        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Contains("error " + ErrorCodes.InvalidArgument, stderr, StringComparison.Ordinal);
    }

    /// <summary>wait-for: --timeout 0 は INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitFor_ZeroTimeout_ReturnsUserError()
    {
        var (_, stderr, exit) = await RunWaitForAsync(["wait-for", "--ref", "s1e1", "--timeout", "0"]);
        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Contains("error " + ErrorCodes.InvalidArgument, stderr, StringComparison.Ordinal);
    }

    /// <summary>wait-for-window: 条件未指定は INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitForWindow_NoConditions_ReturnsUserError()
    {
        var (_, stderr, exit) = await RunWaitForWindowAsync(["wait-for-window"]);
        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Contains("error " + ErrorCodes.InvalidArgument, stderr, StringComparison.Ordinal);
    }

    /// <summary>wait-for-window: --timeout 0 は INVALID_ARGUMENT。</summary>
    [Fact]
    public async Task WaitForWindow_ZeroTimeout_ReturnsUserError()
    {
        var (_, stderr, exit) = await RunWaitForWindowAsync(["wait-for-window", "--title", "x", "--timeout", "0"]);
        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Contains("error " + ErrorCodes.InvalidArgument, stderr, StringComparison.Ordinal);
    }

    /// <summary>wait-for は期待オプションを公開している。</summary>
    [Fact]
    public void WaitFor_ExposesExpectedOptions()
    {
        var cmd = WaitForCommand.Build();
        Assert.Equal("wait-for", cmd.Name);
        foreach (var n in new[] { "--ref", "--name", "--control-type", "--automation-id", "--class-name", "--state", "--timeout", "--sid" })
        {
            Assert.NotNull(cmd.Options.FirstOrDefault(o => o.Name == n));
        }
    }

    /// <summary>wait-for-window は期待オプションを公開している。</summary>
    [Fact]
    public void WaitForWindow_ExposesExpectedOptions()
    {
        var cmd = WaitForWindowCommand.Build();
        Assert.Equal("wait-for-window", cmd.Name);
        foreach (var n in new[] { "--title", "--class-name", "--process-name", "--exe", "--timeout" })
        {
            Assert.NotNull(cmd.Options.FirstOrDefault(o => o.Name == n));
        }
    }

    /// <summary>WaitForCommand.ValidateArgs は組み合わせを期待どおりに弾く。</summary>
    [Theory]
    [InlineData(null, null, null, null, null, null, null, ErrorCodes.InvalidArgument)] // 未指定
    [InlineData("s1e1", "OK", null, null, null, null, null, ErrorCodes.InvalidArgument)] // 排他違反
    [InlineData("bad", null, null, null, null, null, null, ErrorCodes.InvalidRefFormat)] // ref 形式不正
    [InlineData("s1e1", null, null, null, null, "focused", null, ErrorCodes.InvalidArgument)] // 不正 state
    [InlineData("s1e1", null, null, null, null, null, 0, ErrorCodes.InvalidArgument)] // timeout=0
    [InlineData("s1e1", null, null, null, null, "visible", 1000, null)] // 妥当 (ref モード)
    [InlineData(null, "OK", null, null, null, "enabled", null, null)] // 妥当 (検索モード)
    public void WaitFor_ValidateArgs(string? @ref, string? name, string? controlType, string? automationId, string? className, string? state, int? timeoutMs, string? expectedCode)
    {
        var (code, _) = WaitForCommand.ValidateArgs(@ref, name, controlType, automationId, className, state, timeoutMs);
        Assert.Equal(expectedCode, code);
    }

    /// <summary>WaitForWindowCommand.ValidateArgs は条件未指定 / 不正 timeout を弾く。</summary>
    [Theory]
    [InlineData(null, null, null, null, null, ErrorCodes.InvalidArgument)]
    [InlineData("notepad", null, null, null, 0, ErrorCodes.InvalidArgument)]
    [InlineData("notepad", null, null, null, 1000, null)]
    [InlineData(null, null, "notepad", null, null, null)]
    [InlineData(null, null, null, "C:\\\\notepad.exe", null, null)]
    public void WaitForWindow_ValidateArgs(string? title, string? className, string? processName, string? exe, int? timeoutMs, string? expectedCode)
    {
        var (code, _) = WaitForWindowCommand.ValidateArgs(title, className, processName, exe, timeoutMs);
        Assert.Equal(expectedCode, code);
    }

    private static Task<(string stdout, string stderr, int exit)> RunWaitForAsync(string[] args)
        => RunAsync(args, root => root.Subcommands.Add(WaitForCommand.Build()));

    private static Task<(string stdout, string stderr, int exit)> RunWaitForWindowAsync(string[] args)
        => RunAsync(args, root => root.Subcommands.Add(WaitForWindowCommand.Build()));

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
