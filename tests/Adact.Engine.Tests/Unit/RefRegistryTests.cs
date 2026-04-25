using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;
using Xunit;

namespace Adact.Engine.Tests.Unit;

[Trait("Layer", "Unit")]
public class RefRegistryTests
{
    [Fact]
    public void StartNewGeneration_AfterConstruction_IncrementsGeneration()
    {
        var r = new RefRegistry(1);
        Assert.Equal(0, r.Generation);
        r.StartNewGeneration();
        Assert.Equal(1, r.Generation);
        r.StartNewGeneration();
        Assert.Equal(2, r.Generation);
    }

    [Fact]
    public void Register_BeforeStartNewGeneration_Throws()
    {
        var r = new RefRegistry(1);
        Assert.Throws<InvalidOperationException>(() => r.Register(new FakeElement()));
    }

    [Fact]
    public void Register_FirstElement_GetsId1()
    {
        var r = new RefRegistry(1);
        r.StartNewGeneration();
        var refId = r.Register(new FakeElement());
        Assert.Equal("s1g1e1", refId);
    }

    [Fact]
    public void Register_AcrossGenerations_RestartsElementId()
    {
        var r = new RefRegistry(1);
        r.StartNewGeneration();
        r.Register(new FakeElement());
        r.Register(new FakeElement());
        r.StartNewGeneration();
        var refId = r.Register(new FakeElement());
        Assert.Equal("s1g2e1", refId);
    }

    [Fact]
    public void Resolve_CurrentGenerationRef_ReturnsElement()
    {
        var r = new RefRegistry(2);
        r.StartNewGeneration();
        var el = new FakeElement();
        var refId = r.Register(el);
        Assert.Same(el, r.Resolve(refId));
    }

    [Fact]
    public void Resolve_OldGenerationRef_Throws()
    {
        var r = new RefRegistry(1);
        r.StartNewGeneration();
        var oldRef = r.Register(new FakeElement());
        r.StartNewGeneration();
        var ex = Assert.Throws<RefNotFoundException>(() => r.Resolve(oldRef));
        Assert.Contains("generation mismatch", ex.Message);
    }

    [Fact]
    public void Resolve_DifferentSessionRef_Throws()
    {
        var r1 = new RefRegistry(1);
        var r2 = new RefRegistry(2);
        r1.StartNewGeneration();
        r2.StartNewGeneration();
        var s1Ref = r1.Register(new FakeElement());
        var ex = Assert.Throws<RefNotFoundException>(() => r2.Resolve(s1Ref));
        Assert.Contains("session mismatch", ex.Message);
    }

    [Fact]
    public void Resolve_MalformedRef_Throws()
    {
        var r = new RefRegistry(1);
        r.StartNewGeneration();
        Assert.Throws<RefNotFoundException>(() => r.Resolve("garbage"));
        Assert.Throws<RefNotFoundException>(() => r.Resolve("s1g1"));
        Assert.Throws<RefNotFoundException>(() => r.Resolve(""));
    }

    [Fact]
    public void Format_GivenValidComponents_RoundTripsViaParse()
    {
        var s = RefId.Format(3, 7, 42);
        Assert.Equal("s3g7e42", s);
        Assert.True(RefId.TryParse(s, out var sid, out var gen, out var eid));
        Assert.Equal((3, 7, 42), (sid, gen, eid));
    }
}
