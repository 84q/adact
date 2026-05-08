using Adact.Engine.Snapshot;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Ref Stability behavior.</summary>
[Trait("Layer", "Unit")]
public class RefStabilityTests
{
    /// <summary>Performs the Same Runtime Id Across Snapshots Reuses Eid operation.</summary>
    [Fact]
    public void SameRuntimeId_AcrossSnapshots_ReusesEid()
    {
        var r = new RefRegistry(1);
        var rid = new[] { 11, 22, 33 };

        r.BeginSnapshot();
        var first = r.Register(new FakeElement { RuntimeId = rid }, positionalIndex: 0);

        r.BeginSnapshot();
        var second = r.Register(new FakeElement { RuntimeId = rid }, positionalIndex: 5);

        Assert.Equal(first, second);
    }

    /// <summary>Performs the New Runtime Id Gets New Eid operation.</summary>
    [Fact]
    public void NewRuntimeId_GetsNewEid()
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
        var atIndex0 = r.Register(new FakeElement(), positionalIndex: 0);
        var atIndex1 = r.Register(new FakeElement(), positionalIndex: 1);

        Assert.NotEqual(atIndex0, atIndex1);

        r.BeginSnapshot();
        var atIndex0Again = r.Register(new FakeElement(), positionalIndex: 0);
        Assert.Equal(atIndex0, atIndex0Again);
    }

    /// <summary>Performs the Empty Runtime Id Is Treated As Missing operation.</summary>
    [Fact]
    public void EmptyRuntimeId_IsTreatedAsMissing()
    {
        var r = new RefRegistry(1);

        r.BeginSnapshot();
        var first = r.Register(new FakeElement { RuntimeId = Array.Empty<int>() }, positionalIndex: 7);

        r.BeginSnapshot();
        var second = r.Register(new FakeElement { RuntimeId = Array.Empty<int>() }, positionalIndex: 7);

        Assert.Equal(first, second);
    }
}
