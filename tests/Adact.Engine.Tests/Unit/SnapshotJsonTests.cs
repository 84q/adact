using System.Text.Json;
using Adact.Engine.Filters;
using Adact.Engine.Snapshot;
using Xunit;

namespace Adact.Engine.Tests.Unit;

[Trait("Layer", "Unit")]
public class SnapshotJsonTests
{
    private static JsonDocument BuildAndParse(FakeElement root, string filterName = "operable")
    {
        var registry = new RefRegistry(1);
        var filter = new FilterStrategyRegistry().Get(filterName);
        var builder = new SnapshotBuilder(registry);
        var input = new SnapshotBuildInput(
            root, Array.Empty<Adact.Engine.Elements.IElement>(), filter,
            new SnapshotOptions(filterName), root.Name ?? "", "FakeProcess", 1234,
            DateTimeOffset.UnixEpoch);
        var result = builder.Build(input);
        return JsonDocument.Parse(result.Json);
    }

    [Fact]
    public void Build_Produces_MetaWithRequiredFields()
    {
        var root = FakeElement.Window("Test", FakeElement.Button("OK"));
        using var doc = BuildAndParse(root);
        var meta = doc.RootElement.GetProperty("_meta");
        Assert.Equal("operable", meta.GetProperty("filter").GetString());
        Assert.Equal("s1", meta.GetProperty("sessionId").GetString());
        Assert.Equal(1, meta.GetProperty("generation").GetInt32());
        Assert.Equal("Test", meta.GetProperty("windowTitle").GetString());
        Assert.Equal("FakeProcess", meta.GetProperty("processName").GetString());
        Assert.Equal(1234, meta.GetProperty("processId").GetInt32());
        Assert.Equal(JsonValueKind.Null, meta.GetProperty("modalDialog").ValueKind);
    }

    [Fact]
    public void Build_GivenLeafElement_OmitsChildrenKey()
    {
        var root = FakeElement.Window("T");
        using var doc = BuildAndParse(root);
        var tree = doc.RootElement.GetProperty("tree");
        Assert.False(tree.TryGetProperty("children", out _));
    }

    [Fact]
    public void Build_GivenEmptyName_OmitsNameProperty()
    {
        var root = FakeElement.Window("");
        using var doc = BuildAndParse(root);
        var tree = doc.RootElement.GetProperty("tree");
        Assert.False(tree.TryGetProperty("name", out _));
    }

    [Fact]
    public void Build_GivenNonZeroBounds_EmitsBoundingRectAsArray()
    {
        var root = FakeElement.Window("T");
        root.BoundingRectangle = new Rect(10, 20, 300, 400);
        using var doc = BuildAndParse(root);
        var rect = doc.RootElement.GetProperty("tree").GetProperty("boundingRect");
        Assert.Equal(JsonValueKind.Array, rect.ValueKind);
        Assert.Equal(new[] { 10, 20, 300, 400 }, rect.EnumerateArray().Select(e => e.GetInt32()).ToArray());
    }

    [Fact]
    public void Build_GivenMultipleChildren_AssignsSequentialRefIds()
    {
        var root = FakeElement.Window("T",
            FakeElement.Button("a"),
            FakeElement.Button("b"),
            FakeElement.Button("c"));
        using var doc = BuildAndParse(root);
        var children = doc.RootElement.GetProperty("tree").GetProperty("children");
        var refs = children.EnumerateArray().Select(c => c.GetProperty("ref").GetString()).ToArray();
        Assert.Equal(new[] { "s1g1e2", "s1g1e3", "s1g1e4" }, refs);
    }

    [Fact]
    public void Build_RawFilter_IncludesUnnamedPane()
    {
        var root = FakeElement.Window("T",
            FakeElement.Pane(null, FakeElement.Button("inner")));
        using var doc = BuildAndParse(root, "raw");
        var children = doc.RootElement.GetProperty("tree").GetProperty("children");
        Assert.Equal(1, children.GetArrayLength());
        Assert.Equal("Pane", children[0].GetProperty("role").GetString());
    }
}
