using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;

using Xunit;

namespace Adact.Engine.Tests.Unit;

[Trait("Layer", "Unit")]
public class RefRegistryTests
{
    [Fact]
    public void Register_FirstElement_GetsId1()
    {
        var r = new RefRegistry(1);
        r.BeginSnapshot();
        var refId = r.Register(new FakeElement(), positionalIndex: 0);
        Assert.Equal("s1e1", refId);
    }

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

    [Fact]
    public void StableKey_DifferentRuntimeId_AssignsNewEid()
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
        var first = r.Register(new FakeElement(), positionalIndex: 0);

        r.BeginSnapshot();
        // 同じ positional index なら同じ stableKey ("unstable:0") → eid 再利用
        var second = r.Register(new FakeElement(), positionalIndex: 0);

        Assert.Equal(first, second);

        // 別の positional index なら別 eid
        var third = r.Register(new FakeElement(), positionalIndex: 1);
        Assert.NotEqual(first, third);
    }

    [Fact]
    public void Resolve_CurrentSnapshotRef_ReturnsElement()
    {
        var r = new RefRegistry(2);
        r.BeginSnapshot();
        var el = new FakeElement();
        var refId = r.Register(el, positionalIndex: 0);
        Assert.Same(el, r.Resolve(refId));
    }

    [Fact]
    public void Resolve_ElementNotInCurrentSnapshot_Throws()
    {
        var r = new RefRegistry(1);
        r.BeginSnapshot();
        // RuntimeId 無し要素を index 0 で登録
        var refId = r.Register(new FakeElement(), positionalIndex: 0);

        // 新 snapshot では index 0 を登録しない (登録なし)
        r.BeginSnapshot();

        var ex = Assert.Throws<RefNotFoundException>(() => r.Resolve(refId));
        Assert.Contains("not found in current snapshot", ex.Message, StringComparison.Ordinal);
    }

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

    [Fact]
    public void Resolve_MalformedRef_Throws()
    {
        var r = new RefRegistry(1);
        r.BeginSnapshot();
        Assert.Throws<RefNotFoundException>(() => r.Resolve("garbage"));
        Assert.Throws<RefNotFoundException>(() => r.Resolve("s1"));
        Assert.Throws<RefNotFoundException>(() => r.Resolve(""));
    }

    [Fact]
    public void Format_GivenValidComponents_RoundTripsViaParse()
    {
        var s = RefId.Format(3, 42);
        Assert.Equal("s3e42", s);
        Assert.True(RefId.TryParse(s, out var sid, out var eid));
        Assert.Equal((3, 42), (sid, eid));
    }
}
