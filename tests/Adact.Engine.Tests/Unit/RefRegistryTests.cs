using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Ref Registry behavior.</summary>
[Trait("Layer", "Unit")]
public class RefRegistryTests
{
    /// <summary>Performs the Register First Element Gets Id1 operation.</summary>
    [Fact]
    public void Register_FirstElement_GetsId1()
    {
        var r = new RefRegistry(1);
        r.BeginSnapshot();
        var refId = r.Register(new FakeElement(), positionalIndex: 0);
        Assert.Equal("s1e1", refId);
    }

    /// <summary>Performs the Stable Key Same Runtime Id Reuses Eid operation.</summary>
    [Fact]
    public void StableKey_SameRuntimeId_ReusesEid()
    {
        var r = new RefRegistry(1);
        var rid = new[] { 42, 7 };

        r.BeginSnapshot();
        var first = r.Register(new FakeElement { RuntimeId = rid }, positionalIndex: 0);

        r.BeginSnapshot();
        var second = r.Register(new FakeElement { RuntimeId = rid }, positionalIndex: 0);

        Assert.Equal(first, second);
    }

    /// <summary>Performs the Stable Key Different Runtime Id Assigns New Eid operation.</summary>
    [Fact]
    public void StableKey_DifferentRuntimeId_AssignsNewEid()
    {
        var r = new RefRegistry(1);

        r.BeginSnapshot();
        var a = r.Register(new FakeElement { RuntimeId = new[] { 1 } }, positionalIndex: 0);
        var b = r.Register(new FakeElement { RuntimeId = new[] { 2 } }, positionalIndex: 1);

        Assert.NotEqual(a, b);
    }

    /// <summary>Performs the Runtime Id Missing Falls Back To Positional Index operation.</summary>
    [Fact]
    public void RuntimeIdMissing_FallsBackToPositionalIndex()
    {
        var r = new RefRegistry(1);

        r.BeginSnapshot();
        var first = r.Register(new FakeElement(), positionalIndex: 0);

        r.BeginSnapshot();
        var second = r.Register(new FakeElement(), positionalIndex: 0);

        Assert.Equal(first, second);

        var third = r.Register(new FakeElement(), positionalIndex: 1);
        Assert.NotEqual(first, third);
    }

    /// <summary>Resolves the Resolve Current Snapshot Ref Returns Element value.</summary>
    [Fact]
    public void Resolve_CurrentSnapshotRef_ReturnsElement()
    {
        var r = new RefRegistry(2);
        r.BeginSnapshot();
        var el = new FakeElement();
        var refId = r.Register(el, positionalIndex: 0);
        Assert.Same(el, r.Resolve(refId));
    }

    /// <summary>Resolves the Resolve Element Not In Current Snapshot Throws value.</summary>
    [Fact]
    public void Resolve_ElementNotInCurrentSnapshot_Throws()
    {
        var r = new RefRegistry(1);
        r.BeginSnapshot();
        var refId = r.Register(new FakeElement(), positionalIndex: 0);

        r.BeginSnapshot();

        var ex = Assert.Throws<RefNotFoundException>(() => r.Resolve(refId));
        Assert.Contains("not found in current snapshot", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>Resolves the Resolve Different Session Ref Throws value.</summary>
    [Fact]
    public void Resolve_DifferentSessionRef_Throws()
    {
        var r1 = new RefRegistry(1);
        var r2 = new RefRegistry(2);
        r1.BeginSnapshot();
        r2.BeginSnapshot();
        var s1Ref = r1.Register(new FakeElement(), positionalIndex: 0);
        var ex = Assert.Throws<RefNotFoundException>(() => r2.Resolve(s1Ref));
        Assert.Contains("session mismatch", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>Resolves the Resolve Malformed Ref Throws value.</summary>
    [Fact]
    public void Resolve_MalformedRef_Throws()
    {
        var r = new RefRegistry(1);
        r.BeginSnapshot();
        Assert.Throws<RefNotFoundException>(() => r.Resolve("garbage"));
        Assert.Throws<RefNotFoundException>(() => r.Resolve("s1"));
        Assert.Throws<RefNotFoundException>(() => r.Resolve(""));
    }

    /// <summary>Performs the Format Given Valid Components Round Trips Via Parse operation.</summary>
    [Fact]
    public void Format_GivenValidComponents_RoundTripsViaParse()
    {
        var s = RefId.Format(3, 42);
        Assert.Equal("s3e42", s);
        Assert.True(RefId.TryParse(s, out var sid, out var eid));
        Assert.Equal((3, 42), (sid, eid));
    }

    /// <summary>Attempts to perform the Try Parse Invalid Inputs Returns False operation.</summary>
    [Theory]
    [InlineData("s-1e2", false, 0, 0)]
    [InlineData("s1 e2", false, 0, 0)]
    [InlineData("s1e2x", false, 0, 0)]
    [InlineData("se1", false, 0, 0)]
    [InlineData("s1e", false, 0, 0)]
    [InlineData(null, false, 0, 0)]
    [InlineData("", false, 0, 0)]
    [InlineData("S1E2", false, 0, 0)]
    public void TryParse_InvalidInputs_ReturnsFalse(string? value, bool expectedResult, int expectedSid, int expectedEid)
    {
        var result = RefId.TryParse(value!, out var sid, out var eid);
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedSid, sid);
        Assert.Equal(expectedEid, eid);
    }

    /// <summary>Attempts to perform the Try Parse Overflow Boundary Returns False operation.</summary>
    [Theory]
    [InlineData("s2147483648e1")]  // int.MaxValue + 1 for sessionId
    [InlineData("s1e2147483648")]  // int.MaxValue + 1 for elementId
    [InlineData("s4294967295e1")]  // uint.MaxValue for sessionId
    public void TryParse_OverflowBoundary_ReturnsFalse(string value)
    {
        var result = RefId.TryParse(value, out var sid, out var eid);
        Assert.False(result);
        Assert.Equal(0, sid);
        Assert.Equal(0, eid);
    }

    /// <summary>Attempts to perform the Try Parse Leading Zeros Succeeds operation.</summary>
    [Fact]
    public void TryParse_LeadingZeros_Succeeds()
    {
        var result = RefId.TryParse("s01e02", out var sid, out var eid);
        Assert.True(result);
        Assert.Equal(1, sid);
        Assert.Equal(2, eid);
    }
}
