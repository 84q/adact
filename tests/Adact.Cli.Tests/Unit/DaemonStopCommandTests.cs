using System.CommandLine;
using System.Net.Sockets;

using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="DaemonStopCommand"/> の localhost ガード・invalid URL 処理・接続切断例外判定を検証する Unit テスト。
/// daemon-stop のエラー仕様 (Phase5 §8) の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
[Collection(ConsoleCollection.Name)]
public class DaemonStopCommandTests
{
    /// <summary>
    /// 非ローカル URL を --server で渡したとき、接続より前に LOCAL_ONLY エラーと exit=2 (UserError) となることを確認する。
    /// </summary>
    /// <param name="remote">検証対象の非ローカル URL。</param>
    /// <returns>テスト完了タスク。</returns>
    [Theory]
    [InlineData("http://192.168.1.10:41300/mcp")]
    [InlineData("https://example.com/mcp")]
    public async Task DaemonStop_NonLocalhost_ReturnsLocalOnlyAndExit2(string remote)
    {
        var (stdout, stderr, exit) = await RunAsync(["daemon-stop", "--server", remote]);

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("error " + ErrorCodes.LocalOnly, stderr, StringComparison.Ordinal);
        // 接続前に弾けたことの確認 (CONNECTION_FAILED は出ない)。
        Assert.DoesNotContain(ErrorCodes.ConnectionFailed, stderr, StringComparison.Ordinal);
    }

    /// <summary>不正 URL を --server に渡したとき INVALID_ARGUMENT エラーと UserError exit となることを確認する。</summary>
    /// <returns>テスト完了タスク。</returns>
    [Fact]
    public async Task DaemonStop_InvalidUrl_ReturnsUserError()
    {
        var (_, stderr, exit) = await RunAsync(["daemon-stop", "--server", "not-a-url"]);

        Assert.Equal(ExitCodes.UserError, exit);
        Assert.Contains("error " + ErrorCodes.InvalidArgument, stderr, StringComparison.Ordinal);
    }

    // Phase5 #8 M1/m2: CallToolAsync 経路で daemon が落ちた際の切断系例外は benign 扱いとする。
    // CancellationToken 由来 (Ctrl+C) は除外する。
    /// <summary>
    /// daemon が落ちた際の接続切断系例外 (HttpRequest/Socket/IO/ObjectDisposed/InnerException 連鎖) が benign として検出されることを確認する。
    /// Phase5 #8 M1/m2 の「接続切断はエラー扱いしない」仕様の回帰防止。
    /// </summary>
    /// <param name="ex">検証対象の例外。</param>
    [Theory]
    [MemberData(nameof(ConnectionDropCases))]
    public void IsConnectionDropException_ConnectionExceptions_ReturnTrue(Exception ex)
    {
        Assert.True(DaemonStopCommand.IsConnectionDropException(ex));
    }

    /// <summary>キャンセル系 (OperationCanceled / TaskCanceled) とその他の例外は接続切断と見なさないことを確認する。</summary>
    /// <param name="ex">検証対象の例外。</param>
    [Theory]
    [MemberData(nameof(NonConnectionDropCases))]
    public void IsConnectionDropException_OtherExceptions_ReturnFalse(Exception ex)
    {
        Assert.False(DaemonStopCommand.IsConnectionDropException(ex));
    }

    /// <summary>IsConnectionDropException で true を期待する例外ケース。</summary>
    /// <returns>例外ケースの列。</returns>
    public static IEnumerable<object[]> ConnectionDropCases()
    {
        yield return new object[] { new HttpRequestException("boom") };
        yield return new object[] { new SocketException() };
        yield return new object[] { new IOException("io") };
        yield return new object[] { new ObjectDisposedException("x") };
        // InnerException 連鎖でも検出されること
        yield return new object[] { new InvalidOperationException("wrap", new HttpRequestException("inner")) };
    }

    /// <summary>IsConnectionDropException で false を期待する例外ケース。</summary>
    /// <returns>例外ケースの列。</returns>
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
