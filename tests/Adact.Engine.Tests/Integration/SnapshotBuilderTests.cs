using System.Text.Json;

using Adact.Engine.Snapshot;

using Xunit;

namespace Adact.Engine.Tests.Integration;

/// <summary>Contains tests for the Snapshot Builder behavior.</summary>
[Trait("Layer", "Integration")]
public class SnapshotBuilderTests
{
    private static (JsonDocument doc, SnapshotBuildResult result) Build(
        FakeElement root,
        IReadOnlyList<Adact.Engine.Elements.IElement>? modals = null,
        IReadOnlyList<Adact.Engine.Elements.IElement>? popups = null,
        int sessionId = 1)
    {
        var registry = new RefRegistry(sessionId);
        var builder = new SnapshotBuilder(registry);
        var input = new SnapshotBuildInput(
            root, modals ?? Array.Empty<Adact.Engine.Elements.IElement>(),
            popups ?? Array.Empty<Adact.Engine.Elements.IElement>(),
            new SnapshotOptions(),
            WindowTitle: root.Name ?? "",
            ProcessName: "Fake",
            ProcessId: 99,
            GeneratedAt: DateTimeOffset.UnixEpoch);
        var result = builder.Build(input);
        return (JsonDocument.Parse(result.Json), result);
    }

    /// <summary>Performs the Raw Includes Unnamed Pane And Its Children operation.</summary>
    [Fact]
    public void Raw_IncludesUnnamedPaneAndItsChildren()
    {
        var btn = FakeElement.Button("inner");
        var root = FakeElement.Window("T", FakeElement.Pane(null, btn));
        var (doc, _) = Build(root);

        var children = doc.RootElement.GetProperty("tree").GetProperty("children");
        Assert.Equal(1, children.GetArrayLength());
        Assert.Equal("Pane", children[0].GetProperty("role").GetString());
        var grand = children[0].GetProperty("children");
        Assert.Equal(1, grand.GetArrayLength());
        Assert.Equal("Button", grand[0].GetProperty("role").GetString());
    }

    /// <summary>Performs the Raw Offscreen Element Is Still Included operation.</summary>
    [Fact]
    public void Raw_OffscreenElement_IsStillIncluded()
    {
        var hidden = FakeElement.Button("hidden");
        hidden.IsOffscreen = true;
        var visible = FakeElement.Button("visible");
        var root = FakeElement.Window("T", hidden, visible);
        var (doc, _) = Build(root);

        var children = doc.RootElement.GetProperty("tree").GetProperty("children");
        Assert.Equal(2, children.GetArrayLength());
        Assert.True(children[0].GetProperty("isOffscreen").GetBoolean());
    }

    /// <summary>Performs the Build With Popup Siblings Adds Popups To Root Children With Is Popup Flag operation.</summary>
    [Fact]
    public void Build_WithPopupSiblings_AddsPopupsToRootChildren_WithIsPopupFlag()
    {
        var root = FakeElement.Window("Main");
        var popup = FakeElement.Window("PopupWindow");
        var (doc, _) = Build(root, popups: new[] { popup });

        var children = doc.RootElement.GetProperty("tree").GetProperty("children");
        Assert.Equal(1, children.GetArrayLength());
        Assert.Equal("Window", children[0].GetProperty("role").GetString());
        Assert.Equal("PopupWindow", children[0].GetProperty("name").GetString());
        Assert.True(children[0].GetProperty("isPopup").GetBoolean());
    }

    /// <summary>Performs the Raw Selected Element Has Is Selected Flag operation.</summary>
    [Fact]
    public void Raw_SelectedElement_HasIsSelectedFlag()
    {
        var selected = FakeElement.Button("selected");
        selected.IsSelected = true;
        var unselected = FakeElement.Button("unselected");
        var root = FakeElement.Window("T", selected, unselected);
        var (doc, _) = Build(root);

        var children = doc.RootElement.GetProperty("tree").GetProperty("children");
        Assert.Equal(2, children.GetArrayLength());
        Assert.True(children[0].GetProperty("isSelected").GetBoolean());
        Assert.False(children[1].TryGetProperty("isSelected", out _));
    }

    /// <summary>Performs the Ref Ids Within Single Snapshot Are Unique And Sequential operation.</summary>
    [Fact]
    public void RefIds_WithinSingleSnapshot_AreUniqueAndSequential()
    {
        var root = FakeElement.Window("T",
            FakeElement.Pane("A", FakeElement.Button("x")),
            FakeElement.Button("y"));
        var (doc, _) = Build(root);

        var refs = new HashSet<string>();
        Walk(doc.RootElement.GetProperty("tree"), refs);
        Assert.Equal(4, refs.Count);
        Assert.Contains("s1e1", refs);
    }

    private static void Walk(JsonElement node, HashSet<string> refs)
    {
        refs.Add(node.GetProperty("ref").GetString()!);
        if (node.TryGetProperty("children", out var c))
            foreach (var ch in c.EnumerateArray()) Walk(ch, refs);
    }

    /// <summary>Performs the Same Element Across Snapshots Reuses Ref operation.</summary>
    [Fact]
    public void SameElement_AcrossSnapshots_ReusesRef()
    {
        var registry = new RefRegistry(1);
        var builder = new SnapshotBuilder(registry);

        var firstRefs = BuildAndCollectRefs(builder, MakeWindowWithButton());
        var secondRefs = BuildAndCollectRefs(builder, MakeWindowWithButton());

        Assert.Equal(firstRefs, secondRefs);
    }

    /// <summary>Performs the Ref Ids Across Multiple Sessions Do Not Collide operation.</summary>
    [Fact]
    public void RefIds_AcrossMultipleSessions_DoNotCollide()
    {
        var (_, r1) = Build(FakeElement.Window("A", FakeElement.Button("x")), sessionId: 1);
        var (_, r2) = Build(FakeElement.Window("B", FakeElement.Button("y")), sessionId: 2);
        Assert.Equal("s1", r1.SessionId);
        Assert.Equal("s2", r2.SessionId);
        Assert.Contains("s1e", r1.Json, StringComparison.Ordinal);
        Assert.Contains("s2e", r2.Json, StringComparison.Ordinal);
        Assert.DoesNotContain("s2e", r1.Json, StringComparison.Ordinal);
    }

    private static FakeElement MakeWindowWithButton()
    {
        var w = new FakeElement
        {
            ControlType = "Window",
            Name = "T",
            RuntimeId = new[] { 100 },
        };
        var b = new FakeElement
        {
            ControlType = "Button",
            Name = "a",
            RuntimeId = new[] { 200 },
        };
        w.ChildList.Add(b);
        return w;
    }

    private static string[] BuildAndCollectRefs(SnapshotBuilder builder, FakeElement root)
    {
        var input = new SnapshotBuildInput(
            root, Array.Empty<Adact.Engine.Elements.IElement>(),
            Array.Empty<Adact.Engine.Elements.IElement>(),
            new SnapshotOptions(),
            WindowTitle: root.Name ?? "",
            ProcessName: "Fake",
            ProcessId: 1,
            GeneratedAt: DateTimeOffset.UnixEpoch);
        var built = builder.Build(input);
        using var doc = JsonDocument.Parse(built.Json);
        var list = new List<string>();
        Collect(doc.RootElement.GetProperty("tree"), list);
        return list.ToArray();
    }

    private static void Collect(JsonElement node, List<string> refs)
    {
        refs.Add(node.GetProperty("ref").GetString()!);
        if (node.TryGetProperty("children", out var c))
            foreach (var ch in c.EnumerateArray()) Collect(ch, refs);
    }

    /// <summary>Performs the Build Modal Sibling Also In Uia Tree Sets Is Modal Dialog True operation.</summary>
    [Fact]
    public void Build_ModalSiblingAlsoInUiaTree_SetsIsModalDialogTrue()
    {
        var dialog = new FakeElement
        {
            ControlType = "Window",
            Name = "Open",
            RuntimeId = new[] { 300 },
        };
        var button = FakeElement.Button("OK");
        dialog.ChildList.Add(button);

        var root = FakeElement.Window("Main", dialog);

        var (doc, _) = Build(root, modals: new Adact.Engine.Elements.IElement[] { dialog });

        var tree = doc.RootElement.GetProperty("tree");
        var children = tree.GetProperty("children");

        var dialogNodes = new List<JsonElement>();
        foreach (var child in children.EnumerateArray())
        {
            if (child.GetProperty("role").GetString() == "Window"
                && child.TryGetProperty("name", out var n) && n.GetString() == "Open")
            {
                dialogNodes.Add(child);
            }
        }
        Assert.Single(dialogNodes);

        Assert.True(dialogNodes[0].GetProperty("isModalDialog").GetBoolean());

        var meta = doc.RootElement.GetProperty("_meta");
        Assert.True(meta.TryGetProperty("modalDialog", out var modalDialog));
        Assert.Equal(JsonValueKind.Array, modalDialog.ValueKind);
        Assert.Equal(1, modalDialog.GetArrayLength());
        Assert.Equal("Open", modalDialog[0].GetProperty("title").GetString());
    }
}
