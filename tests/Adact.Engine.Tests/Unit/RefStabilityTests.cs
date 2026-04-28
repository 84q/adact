using Adact.Engine.Snapshot;

using Xunit;

namespace Adact.Engine.Tests.Unit;

[Trait("Layer", "Unit")]
public class RefStabilityTests
{
    [Fact]
    public void SameRuntimeId_AcrossSnapshots_ReusesEid()
    {
        var r = new RefRegistry(1);
        var rid = new[] { 11, 22, 33 };

        r.BeginSnapshot();
        var first = r.Register(new FakeElement { RuntimeId = rid }, positionalIndex: 0);

        r.BeginSnapshot();
        // 2 回目の snapshot で別インスタンスでも RuntimeId が同じなら ref は再利用される。
        var second = r.Register(new FakeElement { RuntimeId = rid }, positionalIndex: 5);

        Assert.Equal(first, second);
    }

    [Fact]
    public void NewRuntimeId_GetsNewEid()
    {
        var r = new RefRegistry(1);
        r.BeginSnapshot();
        var a = r.Register(new FakeElement { RuntimeId = new[] { 1 } }, positionalIndex: 0);
        var b = r.Register(new FakeElement { RuntimeId = new[] { 2 } }, positionalIndex: 1);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void RuntimeIdMissing_FallsBackToPositionalIndex()
    {
        var r = new RefRegistry(1);

        r.BeginSnapshot();
        var atIndex0 = r.Register(new FakeElement(), positionalIndex: 0);
        var atIndex1 = r.Register(new FakeElement(), positionalIndex: 1);

        // 同じ positional index なら同じ ref を再利用、別 index なら別 ref。
        Assert.NotEqual(atIndex0, atIndex1);

        r.BeginSnapshot();
        var atIndex0Again = r.Register(new FakeElement(), positionalIndex: 0);
        Assert.Equal(atIndex0, atIndex0Again);
    }

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
