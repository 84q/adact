using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="AttachCommand.ValidateAttachArgs"/> の入力検証ロジックを検証する Unit テスト。
/// CLI の positional ref 必須・<c>w&lt;n&gt;</c> 形式 (cli.md §attach) の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class AttachCommandTests
{
    /// <summary>
    /// ref が未指定のとき INVALID_ARGUMENT エラーと、説明メッセージが返ることを確認する。
    /// 必須 positional 引数の欠落を CLI 段階で検知する仕様の担保。
    /// </summary>
    [Fact]
    public void ValidateAttachArgs_RefNull_ReturnsInvalidArgument()
    {
        var args = new AttachCommand.AttachArgs(null);
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Equal(ErrorCodes.InvalidArgument, code);
        Assert.NotNull(message);
    }

    /// <summary>
    /// ref の値が "w&lt;n&gt;" フォーマットでないときエラーとなり、フォーマットヒントを出すことを確認する。
    /// </summary>
    [Fact]
    public void ValidateAttachArgs_InvalidRefFormat_ReturnsInvalidArgument()
    {
        var args = new AttachCommand.AttachArgs(Ref: "foo");
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Equal(ErrorCodes.InvalidArgument, code);
        Assert.Contains("w<n>", message);
    }

    /// <summary>
    /// 正規の windowRef ("w3" 等) 指定は有効でありエラーにならないことを確認する。
    /// </summary>
    [Fact]
    public void ValidateAttachArgs_RefOnly_Succeeds()
    {
        var args = new AttachCommand.AttachArgs(Ref: "w3");
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Null(code);
        Assert.Null(message);
    }
}
