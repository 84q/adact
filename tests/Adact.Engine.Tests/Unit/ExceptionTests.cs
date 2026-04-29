using Adact.Engine.Exceptions;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// Engine が投げる例外型 (WindowNotFound / AmbiguousAttach / RefNotFound / ElementInteraction) の
/// プロパティ・メッセージ生成を検証する Unit テスト。
/// errors-and-output.md のエラーメッセージ仕様の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class ExceptionTests
{
    /// <summary>
    /// ProcessName クエリで生成された WindowNotFoundException のメッセージにプロセス名が含まれることを確認する。
    /// </summary>
    [Fact]
    public void WindowNotFoundException_GivenProcessNameQuery_MessageContainsProcessName()
    {
        var ex = new WindowNotFoundException(AttachQuery.ByProcess("notepad++"));
        Assert.Contains("notepad++", ex.Message);
    }

    /// <summary>
    /// 空の AttachQuery で生成されたメッセージに "(empty)" マーカーが含まれることを確認する。
    /// </summary>
    [Fact]
    public void WindowNotFoundException_GivenEmptyQuery_MessageContainsEmptyMarker()
    {
        var ex = new WindowNotFoundException(new AttachQuery());
        Assert.Contains("(empty)", ex.Message);
    }

    /// <summary>
    /// AmbiguousAttachException のメッセージに候補件数が含まれ、Candidates プロパティで全件参照できることを確認する。
    /// </summary>
    [Fact]
    public void AmbiguousAttachException_GivenTwoCandidates_MessageMentionsCount()
    {
        var c = new[]
        {
            new WindowInfo(1, "x", "t1", "Window", null, IntPtr.Zero),
            new WindowInfo(2, "x", "t2", "Window", null, IntPtr.Zero),
        };
        var ex = new AmbiguousAttachException(AttachQuery.ByProcess("x"), c);
        Assert.Contains("2", ex.Message);
        Assert.Equal(2, ex.Candidates.Count);
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
