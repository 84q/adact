using System.CommandLine;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="ResizeCommand"/> の引数検証 (Phase 8 Step 5) を検証する Unit テスト。
/// 接続前 (parser / SetAction 段階) で弾かれる --width / --height の不正値が UserError exit と
/// INVALID_ARGUMENT エラーを返すことを確認する。実 daemon / UIA への接続は行わない。
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class ResizeCommandTests
{
    /// <summary>
    /// --width が 0 以下の場合、接続前に UserError exit と INVALID_ARGUMENT エラーが返ることを確認する。
    /// </summary>
    /// <param name="width">検証対象の不正な width。</param>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Resize_NonPositiveWidth_ReturnsUserError(string width)
    {
        var (_, stderr, exit) = await RunAsync(["resize", "--width", width, "--height", "100"]);

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Contains("error " + ErrorCodes.InvalidArgument, stderr, StringComparison.Ordinal);
        Assert.Contains("--width", stderr, StringComparison.Ordinal);
    }

    /// <summary>
    /// --height が 0 以下の場合、接続前に UserError exit と INVALID_ARGUMENT エラーが返ることを確認する。
    /// </summary>
    /// <param name="height">検証対象の不正な height。</param>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Resize_NonPositiveHeight_ReturnsUserError(string height)
    {
        var (_, stderr, exit) = await RunAsync(["resize", "--width", "100", "--height", height]);

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Contains("error " + ErrorCodes.InvalidArgument, stderr, StringComparison.Ordinal);
        Assert.Contains("--height", stderr, StringComparison.Ordinal);
    }

    /// <summary>
    /// --width 必須オプションを省略すると System.CommandLine の parse error として UserError exit になることを確認する。
    /// </summary>
    [Fact]
    public async Task Resize_MissingWidth_ReturnsUserError()
    {
        var (_, _, exit) = await RunAsync(["resize", "--height", "100"]);

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
            root.Subcommands.Add(ResizeCommand.Build());
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
