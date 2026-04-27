using Adact.Engine.Exceptions;

using Xunit;

namespace Adact.Engine.Tests.Unit;

[Trait("Layer", "Unit")]
public class ExceptionTests
{
    [Fact]
    public void WindowNotFoundException_GivenProcessNameQuery_MessageContainsProcessName()
    {
        var ex = new WindowNotFoundException(AttachQuery.ByProcess("notepad++"));
        Assert.Contains("notepad++", ex.Message);
    }

    [Fact]
    public void WindowNotFoundException_GivenEmptyQuery_MessageContainsEmptyMarker()
    {
        var ex = new WindowNotFoundException(new AttachQuery());
        Assert.Contains("(empty)", ex.Message);
    }

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

    [Fact]
    public void RefNotFoundException_GivenRefIdAndReason_PreservesBothInProperties()
    {
        var ex = new RefNotFoundException("s1g3e7", "generation mismatch");
        Assert.Equal("s1g3e7", ex.RefId);
        Assert.Equal("generation mismatch", ex.Reason);
        Assert.Contains("s1g3e7", ex.Message);
        Assert.Contains("generation mismatch", ex.Message);
    }

    [Fact]
    public void ElementInteractionException_GivenOperationName_PreservesOperationProperty()
    {
        var ex = new ElementInteractionException("s1g1e2", "click", "boom");
        Assert.Equal("click", ex.Operation);
        Assert.Contains("click", ex.Message);
        Assert.Contains("boom", ex.Message);
    }

    [Fact]
    public void FilterStrategyNotFoundException_GivenStrategyName_PreservesNameProperty()
    {
        var ex = new FilterStrategyNotFoundException("foo");
        Assert.Equal("foo", ex.Name);
        Assert.Contains("foo", ex.Message);
    }
}
