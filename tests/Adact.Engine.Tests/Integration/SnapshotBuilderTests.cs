using System.Text.Json;

using Adact.Engine.Snapshot;

using Xunit;

namespace Adact.Engine.Tests.Integration;

/// <summary>
/// <see cref="SnapshotBuilder"/> の raw JSON 生成・ref 採番・セッション間独立性を検証する Integration テスト。
/// FakeElement ツリーを使い、snapshot.md / ref-ids.md の仕様の回帰を防ぐ。
/// </summary>
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

    /// <summary>
    /// Phase 7 の raw 出力で Name なし Pane やその子要素が flatten されずそのまま tree に出ることを確認する。
    /// flatten/Exclude を CLI 側に移譲した仕様変更の回帰防止。
    /// </summary>
    [Fact]
    public void Raw_IncludesUnnamedPaneAndItsChildren()
    {
        // Phase 7: SnapshotBuilder は raw 全要素出力。flatten / Exclude は CLI 側に移譲。
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

    /// <summary>
    /// IsOffscreen=true の要素も raw 出力に含まれ、isOffscreen フラグ付きで出ることを確認する。
    /// 画面外要素を builder が勝手に除外しない仕様の回帰防止。
    /// </summary>
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

    /// <summary>
    /// PopupSiblings を渡した際、root の children に Popup 要素が追加され、
    /// isPopup: true フラグが付与されることを確認する。
    /// </summary>
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

    /// <summary>
    /// IsSelected=true の要素に isSelected フラグが付与され、false の要素には付与されないことを確認する。
    /// </summary>
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

    /// <summary>
    /// 単一 snapshot 内で全ノードの ref がユニークで連番採番 (s1e1, s1e2, ...) されることを確認する。
    /// </summary>
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
        Assert.Contains("s1e1", refs);
    }

    private static void Walk(JsonElement node, HashSet<string> refs)
    {
        refs.Add(node.GetProperty("ref").GetString()!);
        if (node.TryGetProperty("children", out var c))
            foreach (var ch in c.EnumerateArray()) Walk(ch, refs);
    }

    /// <summary>
    /// 同じ RuntimeId の要素ツリーを 2 回ビルドすると同じ ref セットが復元されることを確認する。
    /// snapshot 間で要素 ref が安定していること (ref-ids.md) の回帰防止。
    /// </summary>
    [Fact]
    public void SameElement_AcrossSnapshots_ReusesRef()
    {
        var registry = new RefRegistry(1);
        var builder = new SnapshotBuilder(registry);

        // 同一 RuntimeId を持つ要素ツリーを 2 回 Build する。RuntimeId が同一なら ref は再利用される。
        var firstRefs = BuildAndCollectRefs(builder, MakeWindowWithButton());
        var secondRefs = BuildAndCollectRefs(builder, MakeWindowWithButton());

        Assert.Equal(firstRefs, secondRefs);
    }

    /// <summary>
    /// 異なる sessionId 間では ref 名前空間が独立 (s1e* / s2e*) して衝突しないことを確認する。
    /// 複数 attach 並行時に ref が混在しない仕様の回帰防止。
    /// </summary>
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

    /// <summary>
    /// UIA ツリー上メインウィンドウの子かつ ModalSiblings にも含まれる要素に
    /// isModalDialog: true が設定され、_meta.modalDialog にもエントリが存在し、
    /// 要素が重複出力されないことを確認する。FileDialog 相当のモーダル検出の回帰防止。
    /// </summary>
    [Fact]
    public void Build_ModalSiblingAlsoInUiaTree_SetsIsModalDialogTrue()
    {
        // FileDialog を模倣: UIA ツリー上は root の子 Window だが、
        // Win32 モーダルなので ModalSiblings にも含まれる
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

        // dialog が 1 回だけ出力されていること（重複なし）
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

        // isModalDialog: true が設定されていること
        Assert.True(dialogNodes[0].GetProperty("isModalDialog").GetBoolean());

        // _meta.modalDialog にエントリが存在すること
        var meta = doc.RootElement.GetProperty("_meta");
        Assert.True(meta.TryGetProperty("modalDialog", out var modalDialog));
        Assert.Equal(JsonValueKind.Array, modalDialog.ValueKind);
        Assert.Equal(1, modalDialog.GetArrayLength());
        Assert.Equal("Open", modalDialog[0].GetProperty("title").GetString());
    }
}
