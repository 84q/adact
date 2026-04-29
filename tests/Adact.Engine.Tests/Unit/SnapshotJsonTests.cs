using System.Text.Json;

using Adact.Engine.Snapshot;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// SnapshotBuilder が生成する JSON の _meta / tree フィールドスキーマを検証する Unit テスト。
/// snapshot.md の JSON 仕様 (必須フィールド / 省略規則 / boundingRect 表現) の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class SnapshotJsonTests
{
    private static JsonDocument BuildAndParse(FakeElement root)
    {
        var registry = new RefRegistry(1);
        var builder = new SnapshotBuilder(registry);
        var input = new SnapshotBuildInput(
            root, Array.Empty<Adact.Engine.Elements.IElement>(),
            new SnapshotOptions(), root.Name ?? "", "FakeProcess", 1234,
            DateTimeOffset.UnixEpoch);
        var result = builder.Build(input);
        return JsonDocument.Parse(result.Json);
    }

    /// <summary>
    /// _meta に sessionId / windowTitle / processName / processId が含まれ、modalDialog は null として出され、filter / generation は出ないことを確認する。
    /// </summary>
    [Fact]
    public void Build_Produces_MetaWithRequiredFields()
    {
        var root = FakeElement.Window("Test", FakeElement.Button("OK"));
        using var doc = BuildAndParse(root);
        var meta = doc.RootElement.GetProperty("_meta");
        Assert.Equal("s1", meta.GetProperty("sessionId").GetString());
        Assert.False(meta.TryGetProperty("filter", out _));
        Assert.False(meta.TryGetProperty("generation", out _));
        Assert.Equal("Test", meta.GetProperty("windowTitle").GetString());
        Assert.Equal("FakeProcess", meta.GetProperty("processName").GetString());
        Assert.Equal(1234, meta.GetProperty("processId").GetInt32());
        Assert.Equal(JsonValueKind.Null, meta.GetProperty("modalDialog").ValueKind);
    }

    /// <summary>
    /// 子要素を持たない leaf には children キーが出力されないことを確認する (空配列を出さない仕様)。
    /// </summary>
    [Fact]
    public void Build_GivenLeafElement_OmitsChildrenKey()
    {
        var root = FakeElement.Window("T");
        using var doc = BuildAndParse(root);
        var tree = doc.RootElement.GetProperty("tree");
        Assert.False(tree.TryGetProperty("children", out _));
    }

    /// <summary>
    /// Name が空文字列の要素では name プロパティを出力しないことを確認する。
    /// </summary>
    [Fact]
    public void Build_GivenEmptyName_OmitsNameProperty()
    {
        var root = FakeElement.Window("");
        using var doc = BuildAndParse(root);
        var tree = doc.RootElement.GetProperty("tree");
        Assert.False(tree.TryGetProperty("name", out _));
    }

    /// <summary>
    /// boundingRect がゼロ以外のとき [x,y,w,h] の整数配列として出力されることを確認する。
    /// </summary>
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

    /// <summary>
    /// 兄弟要素に順番に ref (s1e2, s1e3, s1e4) が振られることを確認する。
    /// </summary>
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
        Assert.Equal(new[] { "s1e2", "s1e3", "s1e4" }, refs);
    }

    /// <summary>
    /// raw 出力で Name 無し Pane も flatten されずそのまま出てくることを確認する (Phase 7 仕様変更の回帰防止)。
    /// </summary>
    [Fact]
    public void Build_RawAllElements_IncludesUnnamedPane()
    {
        // Phase 7: raw 全要素 JSON 出力。Pane (Name 無し) も flatten せずそのまま出る。
        var root = FakeElement.Window("T",
            FakeElement.Pane(null, FakeElement.Button("inner")));
        using var doc = BuildAndParse(root);
        var children = doc.RootElement.GetProperty("tree").GetProperty("children");
        Assert.Equal(1, children.GetArrayLength());
        Assert.Equal("Pane", children[0].GetProperty("role").GetString());
    }
}
