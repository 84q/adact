using System.Text.Json;
using Adact.Engine.Filters;
using Adact.Engine.Snapshot;
using Xunit;

namespace Adact.Engine.Tests.Integration;

[Trait("Layer", "Integration")]
public class SnapshotBuilderTests
{
    private static (JsonDocument doc, SnapshotBuildResult result) Build(
        FakeElement root, string filterName = "operable",
        IReadOnlyList<Adact.Engine.Elements.IElement>? modals = null,
        int sessionId = 1)
    {
        var registry = new RefRegistry(sessionId);
        var filter = new FilterStrategyRegistry().Get(filterName);
        var builder = new SnapshotBuilder(registry);
        var input = new SnapshotBuildInput(
            root, modals ?? Array.Empty<Adact.Engine.Elements.IElement>(), filter,
            new SnapshotOptions(filterName),
            WindowTitle: root.Name ?? "",
            ProcessName: "Fake",
            ProcessId: 99,
            GeneratedAt: DateTimeOffset.UnixEpoch);
        var result = builder.Build(input);
        return (JsonDocument.Parse(result.Json), result);
    }

    [Fact]
    public void Operable_FlattensUnnamedPane_AndPromotesGrandchildren()
    {
        var btn = FakeElement.Button("inner");
        var root = FakeElement.Window("T", FakeElement.Pane(null, btn));
        var (doc, _) = Build(root);

        var children = doc.RootElement.GetProperty("tree").GetProperty("children");
        Assert.Equal(1, children.GetArrayLength());
        Assert.Equal("Button", children[0].GetProperty("role").GetString());
        Assert.Equal("inner", children[0].GetProperty("name").GetString());
    }

    [Fact]
    public void Operable_NamedPane_IsKeptAsContainer()
    {
        var root = FakeElement.Window("T",
            FakeElement.Pane("Named",
                FakeElement.Button("inner")));
        var (doc, _) = Build(root);

        var children = doc.RootElement.GetProperty("tree").GetProperty("children");
        Assert.Equal(1, children.GetArrayLength());
        Assert.Equal("Pane", children[0].GetProperty("role").GetString());
        Assert.Equal("Named", children[0].GetProperty("name").GetString());
        Assert.Equal("Button", children[0].GetProperty("children")[0].GetProperty("role").GetString());
    }

    [Fact]
    public void Operable_OffscreenElement_IsExcluded()
    {
        var hidden = FakeElement.Button("hidden");
        hidden.IsOffscreen = true;
        var visible = FakeElement.Button("visible");
        var root = FakeElement.Window("T", hidden, visible);
        var (doc, _) = Build(root);

        var children = doc.RootElement.GetProperty("tree").GetProperty("children");
        Assert.Equal(1, children.GetArrayLength());
        Assert.Equal("visible", children[0].GetProperty("name").GetString());
    }

    [Fact]
    public void Operable_NestedUnnamedPanes_AreFlattened()
    {
        var btn = FakeElement.Button("deep");
        var root = FakeElement.Window("T",
            FakeElement.Pane(null,
                FakeElement.Pane(null, btn)));
        var (doc, _) = Build(root);

        var children = doc.RootElement.GetProperty("tree").GetProperty("children");
        Assert.Equal(1, children.GetArrayLength());
        Assert.Equal("Button", children[0].GetProperty("role").GetString());
    }

    [Fact]
    public void RefIds_WithinSingleSnapshot_AreUniqueAndSequential()
    {
        var root = FakeElement.Window("T",
            FakeElement.Pane("A", FakeElement.Button("x")),
            FakeElement.Button("y"));
        var (doc, _) = Build(root);

        var refs = new HashSet<string>();
        Walk(doc.RootElement.GetProperty("tree"), refs);
        // Window + Pane + Button(x) + Button(y) = 4 ノード
        Assert.Equal(4, refs.Count);
        Assert.Contains("s1g1e1", refs);
    }

    private static void Walk(JsonElement node, HashSet<string> refs)
    {
        refs.Add(node.GetProperty("ref").GetString()!);
        if (node.TryGetProperty("children", out var c))
            foreach (var ch in c.EnumerateArray()) Walk(ch, refs);
    }

    [Fact]
    public void OldGenerationRef_AfterRebuild_IsRejected()
    {
        var registry = new RefRegistry(1);
        var filter = new FilterStrategyRegistry().Get("operable");
        var builder = new SnapshotBuilder(registry);

        var root1 = FakeElement.Window("T", FakeElement.Button("a"));
        builder.Build(new SnapshotBuildInput(
            root1, Array.Empty<Adact.Engine.Elements.IElement>(), filter,
            new SnapshotOptions(), "T", "Fake", 1, DateTimeOffset.UnixEpoch));

        // この時点では g1 解決可能
        Assert.NotNull(registry.Resolve("s1g1e1"));

        // 再 Build → 旧 refId は無効
        builder.Build(new SnapshotBuildInput(
            root1, Array.Empty<Adact.Engine.Elements.IElement>(), filter,
            new SnapshotOptions(), "T", "Fake", 1, DateTimeOffset.UnixEpoch));

        var ex = Assert.Throws<Exceptions.RefNotFoundException>(() => registry.Resolve("s1g1e1"));
        Assert.Contains("generation mismatch", ex.Message);

        // 新世代の refId は解決可能
        Assert.NotNull(registry.Resolve("s1g2e1"));
    }

    [Fact]
    public void RefIds_AcrossMultipleSessions_DoNotCollide()
    {
        var (_, r1) = Build(FakeElement.Window("A", FakeElement.Button("x")), sessionId: 1);
        var (_, r2) = Build(FakeElement.Window("B", FakeElement.Button("y")), sessionId: 2);
        Assert.Equal("s1", r1.SessionId);
        Assert.Equal("s2", r2.SessionId);
        Assert.Contains("s1g1", r1.Json);
        Assert.Contains("s2g1", r2.Json);
        Assert.DoesNotContain("s2g1", r1.Json);
    }
}
