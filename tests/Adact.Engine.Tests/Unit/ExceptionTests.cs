using Adact.Engine.Exceptions;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// Engine が投げる例外型 (WindowNotFound / RefNotFound / ElementInteraction) の
/// プロパティ・メッセージ生成を検証する Unit テスト。
/// errors-and-output.md のエラーメッセージ仕様の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class ExceptionTests
{
    /// <summary>
    /// HWND 指定で生成された WindowNotFoundException のメッセージに HWND 値が 16 進で含まれることを確認する。
    /// HWND ベース attach 失敗の診断情報がエラーメッセージから読めることの担保。
    /// </summary>
    [Fact]
    public void WindowNotFoundException_GivenHwnd_MessageContainsHwndHex()
    {
        var ex = new WindowNotFoundException(new IntPtr(0xABCD));
        Assert.Equal((nint)0xABCD, ex.Hwnd);
        Assert.Contains("ABCD", ex.Message);
    }

    /// <summary>
    /// RefNotFoundException の RefId/Reason プロパティが保持され、メッセージに両方が含まれることを確認する。
    /// </summary>
    [Fact]
    public void RefNotFoundException_GivenRefIdAndReason_PreservesBothInProperties()
    {
        var ex = new RefNotFoundException("s1e7", "not found in current snapshot");
        Assert.Equal("s1e7", ex.RefId);
        Assert.Equal("not found in current snapshot", ex.Reason);
        Assert.Contains("s1e7", ex.Message);
        Assert.Contains("not found in current snapshot", ex.Message);
    }

    /// <summary>
    /// ElementInteractionException の Operation プロパティが保持され、メッセージに operation/reason が含まれることを確認する。
    /// </summary>
    [Fact]
    public void ElementInteractionException_GivenOperationName_PreservesOperationProperty()
    {
        var ex = new ElementInteractionException("s1e2", "click", "boom");
        Assert.Equal("click", ex.Operation);
        Assert.Contains("click", ex.Message);
        Assert.Contains("boom", ex.Message);
    }
}
