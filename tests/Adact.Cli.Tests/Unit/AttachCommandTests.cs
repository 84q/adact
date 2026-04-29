using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="AttachCommand.ValidateAttachArgs"/> の入力検証ロジックを検証する Unit テスト。
/// CLI の --ref / --process / --title etc. の排他・必須ルール (cli.md §attach) の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class AttachCommandTests
{
    /// <summary>
    /// 全フィールド未指定のとき INVALID_ARGUMENT エラーと、説明メッセージが返ることを確認する。
    /// </summary>
    [Fact]
    public void ValidateAttachArgs_AllNull_ReturnsInvalidArgument()
    {
        var args = new AttachCommand.AttachArgs(null, null, null, null, null);
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Equal(ErrorCodes.InvalidArgument, code);
        Assert.NotNull(message);
    }

    /// <summary>
    /// --ref とフラグ系オプションを同時指定したとき INVALID_ARGUMENT となり、"mutually exclusive" メッセージを返すことを確認する。
    /// </summary>
    [Fact]
    public void ValidateAttachArgs_RefAndFlag_ReturnsInvalidArgument()
    {
        var args = new AttachCommand.AttachArgs(Ref: "w3", ProcessName: "calc", Title: null, ProcessId: null, ClassName: null);
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Equal(ErrorCodes.InvalidArgument, code);
        Assert.Contains("mutually exclusive", message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// --ref の値が "w&lt;n&gt;" フォーマットでないときエラーとなり、フォーマットヒントを出すことを確認する。
    /// </summary>
    [Fact]
    public void ValidateAttachArgs_InvalidRefFormat_ReturnsInvalidArgument()
    {
        var args = new AttachCommand.AttachArgs(Ref: "foo", ProcessName: null, Title: null, ProcessId: null, ClassName: null);
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Equal(ErrorCodes.InvalidArgument, code);
        Assert.Contains("w<n>", message);
    }

    /// <summary>
    /// --ref のみ指定は有効でありエラーにならないことを確認する。
    /// </summary>
    [Fact]
    public void ValidateAttachArgs_RefOnly_Succeeds()
    {
        var args = new AttachCommand.AttachArgs(Ref: "w3", ProcessName: null, Title: null, ProcessId: null, ClassName: null);
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Null(code);
        Assert.Null(message);
    }

    /// <summary>
    /// --process のみ指定は有効であることを確認する。
    /// </summary>
    [Fact]
    public void ValidateAttachArgs_ProcessNameOnly_Succeeds()
    {
        var args = new AttachCommand.AttachArgs(Ref: null, ProcessName: "calc", Title: null, ProcessId: null, ClassName: null);
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Null(code);
        Assert.Null(message);
    }

    /// <summary>
    /// --process と --title の併用は AND 条件として受け付けられることを確認する (排他ではない)。
    /// </summary>
    [Fact]
    public void ValidateAttachArgs_ProcessNameAndTitle_Succeeds()
    {
        // 複数フラグ指定は AND 条件 (排他ではない)。
        var args = new AttachCommand.AttachArgs(Ref: null, ProcessName: "calc", Title: "電卓", ProcessId: null, ClassName: null);
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Null(code);
        Assert.Null(message);
    }
}
