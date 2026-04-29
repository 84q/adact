using System.Text.Json;

using Adact.Engine.Snapshot;

using Xunit;

namespace Adact.Engine.Tests.Integration;

/// <summary>
/// SnapshotBuilder のモーダルダイアログ検出ロジックを検証する Integration テスト。
/// modal をルートの兄弟として受け取り、JSON 出力と _meta に反映される仕様 (snapshot.md) の回帰防止。
/// </summary>
[Trait("Layer", "Integration")]
public class ModalDialogDetectionTests
{
    /// <summary>
    /// modal シブリングを与えると、ルートノードの子として isModalDialog=true で追加され、_meta.modalDialog が埋まることを確認する。
    /// </summary>
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

    /// <summary>
    /// modal が無い場合、_meta.modalDialog は null として出力されることを確認する。
    /// modal なしケースでプロパティが欠落したり異なる型になったりしない仕様の回帰防止。
    /// </summary>
    [Fact]
    public void Build_GivenNoModals_ProducesNullModalDialogMeta()
    {
        var root = FakeElement.Window("Main", FakeElement.Button("OK"));
        var registry = new RefRegistry(1);
        var builder = new SnapshotBuilder(registry);
        var input = new SnapshotBuildInput(
            root, Array.Empty<Adact.Engine.Elements.IElement>(),
            new SnapshotOptions(), "Main", "Fake", 1, DateTimeOffset.UnixEpoch);
        var result = builder.Build(input);

        using var doc = JsonDocument.Parse(result.Json);
        Assert.Equal(JsonValueKind.Null,
            doc.RootElement.GetProperty("_meta").GetProperty("modalDialog").ValueKind);
    }
}
