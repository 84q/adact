using System.Text.Json;
using Adact.Engine.Filters;
using Adact.Engine.Snapshot;
using Xunit;

namespace Adact.Engine.Tests.Integration;

[Trait("Layer", "Integration")]
public class ModalDialogDetectionTests
{
  [Fact]
  public void ModalSiblings_AreAddedAsChildrenOfRoot_WithIsModalDialogTrue()
  {
    var root = FakeElement.Window("Main", FakeElement.Button("OK"));
    var modal = FakeElement.Window("Save?",
        FakeElement.Button("Yes"),
        FakeElement.Button("No"));

    var registry = new RefRegistry(1);
    var filter = new FilterStrategyRegistry().Get("operable");
    var builder = new SnapshotBuilder(registry);
    var input = new SnapshotBuildInput(
        root, new Adact.Engine.Elements.IElement[] { modal }, filter,
        new SnapshotOptions(), "Main", "Fake", 1, DateTimeOffset.UnixEpoch);
    var result = builder.Build(input);

    using var doc = JsonDocument.Parse(result.Json);
    var meta = doc.RootElement.GetProperty("_meta");
    var modalsMeta = meta.GetProperty("modalDialog");
    Assert.Equal(JsonValueKind.Array, modalsMeta.ValueKind);
    Assert.Equal(1, modalsMeta.GetArrayLength());
    Assert.Equal("Save?", modalsMeta[0].GetProperty("title").GetString());

    var rootChildren = doc.RootElement.GetProperty("tree").GetProperty("children");
    // OK ボタン + モーダルウィンドウの 2 つ (順序: 通常子 → モーダル)
    Assert.Equal(2, rootChildren.GetArrayLength());
    var modalNode = rootChildren[1];
    Assert.True(modalNode.GetProperty("isModalDialog").GetBoolean());
    Assert.Equal("Window", modalNode.GetProperty("role").GetString());
    Assert.Equal("Save?", modalNode.GetProperty("name").GetString());
    // モーダル内のボタンも refId 付きで含まれる
    Assert.Equal(2, modalNode.GetProperty("children").GetArrayLength());
  }

  [Fact]
  public void Build_GivenNoModals_ProducesNullModalDialogMeta()
  {
    var root = FakeElement.Window("Main", FakeElement.Button("OK"));
    var registry = new RefRegistry(1);
    var filter = new FilterStrategyRegistry().Get("operable");
    var builder = new SnapshotBuilder(registry);
    var input = new SnapshotBuildInput(
        root, Array.Empty<Adact.Engine.Elements.IElement>(), filter,
        new SnapshotOptions(), "Main", "Fake", 1, DateTimeOffset.UnixEpoch);
    var result = builder.Build(input);

    using var doc = JsonDocument.Parse(result.Json);
    Assert.Equal(JsonValueKind.Null,
        doc.RootElement.GetProperty("_meta").GetProperty("modalDialog").ValueKind);
  }
}
