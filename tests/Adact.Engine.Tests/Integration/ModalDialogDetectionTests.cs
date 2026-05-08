using System.Text.Json;

using Adact.Engine.Snapshot;

using Xunit;

namespace Adact.Engine.Tests.Integration;

/// <summary>Contains tests for the Modal Dialog Detection behavior.</summary>
[Trait("Layer", "Integration")]
public class ModalDialogDetectionTests
{
    /// <summary>Performs the Modal Siblings Are Added As Children Of Root With Is Modal Dialog True operation.</summary>
    [Fact]
    public void ModalSiblings_AreAddedAsChildrenOfRoot_WithIsModalDialogTrue()
    {
        var root = FakeElement.Window("Main", FakeElement.Button("OK"));
        var modal = FakeElement.Window("Save?",
            FakeElement.Button("Yes"),
            FakeElement.Button("No"));

        var registry = new RefRegistry(1);
        var builder = new SnapshotBuilder(registry);
        var input = new SnapshotBuildInput(
            root, new Adact.Engine.Elements.IElement[] { modal },
            Array.Empty<Adact.Engine.Elements.IElement>(),
            new SnapshotOptions(), "Main", "Fake", 1, DateTimeOffset.UnixEpoch);
        var result = builder.Build(input);

        using var doc = JsonDocument.Parse(result.Json);
        var meta = doc.RootElement.GetProperty("_meta");
        var modalsMeta = meta.GetProperty("modalDialog");
        Assert.Equal(JsonValueKind.Array, modalsMeta.ValueKind);
        Assert.Equal(1, modalsMeta.GetArrayLength());
        Assert.Equal("Save?", modalsMeta[0].GetProperty("title").GetString());

        var rootChildren = doc.RootElement.GetProperty("tree").GetProperty("children");
        Assert.Equal(2, rootChildren.GetArrayLength());
        var modalNode = rootChildren[1];
        Assert.True(modalNode.GetProperty("isModalDialog").GetBoolean());
        Assert.Equal("Window", modalNode.GetProperty("role").GetString());
        Assert.Equal("Save?", modalNode.GetProperty("name").GetString());
        Assert.Equal(2, modalNode.GetProperty("children").GetArrayLength());
    }

    /// <summary>Performs the Build Given No Modals Produces Null Modal Dialog Meta operation.</summary>
    [Fact]
    public void Build_GivenNoModals_ProducesNullModalDialogMeta()
    {
        var root = FakeElement.Window("Main", FakeElement.Button("OK"));
        var registry = new RefRegistry(1);
        var builder = new SnapshotBuilder(registry);
        var input = new SnapshotBuildInput(
            root, Array.Empty<Adact.Engine.Elements.IElement>(),
            Array.Empty<Adact.Engine.Elements.IElement>(),
            new SnapshotOptions(), "Main", "Fake", 1, DateTimeOffset.UnixEpoch);
        var result = builder.Build(input);

        using var doc = JsonDocument.Parse(result.Json);
        Assert.Equal(JsonValueKind.Null,
            doc.RootElement.GetProperty("_meta").GetProperty("modalDialog").ValueKind);
    }
}
