using Adact.Cli.Snapshots;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="SnapshotTextFormatter.Format"/> の frontmatter / 属性順序 / インデント / エスケープを検証する Unit テスト。
/// snapshot.md のテキスト出力フォーマット仕様の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class SnapshotTextFormatterTests
{
    private static SnapshotElement Leaf(
        string role, string? name = null, string? aid = null,
        string? value = null, bool isEnabled = true,
        bool selected = false, bool focused = false, bool modal = false,
        string refId = "s1e1")
        => new(role, name, aid, Value: value,
            IsEnabled: isEnabled, IsSelected: selected, IsOffscreen: false, HasKeyboardFocus: focused,
            IsModalDialog: modal, Ref: refId, Children: Array.Empty<SnapshotElement>());

    private static SnapshotMeta Meta() =>
        new("s1", "notepad", 1234, "2025-01-01T00:00:00Z");

    /// <summary>frontmatter 先頭に filter・sessionId・processName・processId・generatedAt が期待順で出力されることを確認する。</summary>
    [Fact]
    public void Format_EmitsFrontmatterWithFilter()
    {
        var root = Leaf("Window", "T", refId: "s1e1");
        var text = SnapshotTextFormatter.Format(Meta(), root, "operable");

        Assert.StartsWith("---\nfilter: operable\nsessionId: s1\n", text);
        Assert.Contains("processName: notepad\n", text);
        Assert.Contains("processId: 1234\n", text);
        Assert.Contains("generatedAt: \"2025-01-01T00:00:00Z\"\n", text);
    }

    /// <summary>属性の出力順序が [aid=...] → [value=...] → [disabled] → [focused] → [ref=...] という仕様順を保つことを確認する。</summary>
    [Fact]
    public void Format_AttributeOrder_IsAidValueStateRef()
    {
        var root = Leaf("Edit", null, aid: "field", value: "hello",
            isEnabled: false, focused: true, refId: "s1e7");
        var text = SnapshotTextFormatter.Format(Meta(), root, "operable");

        var line = text.Split('\n').First(l => l.TrimStart().StartsWith("- Edit", StringComparison.Ordinal));
        var idxAid = line.IndexOf("[aid=", StringComparison.Ordinal);
        var idxValue = line.IndexOf("[value=", StringComparison.Ordinal);
        var idxDisabled = line.IndexOf("[disabled]", StringComparison.Ordinal);
        var idxFocused = line.IndexOf("[focused]", StringComparison.Ordinal);
        var idxRef = line.IndexOf("[ref=", StringComparison.Ordinal);

        Assert.True(idxAid >= 0 && idxValue > idxAid && idxDisabled > idxValue
            && idxFocused > idxDisabled && idxRef > idxFocused, line);
    }

    /// <summary>aid/value/disabled/focused が無いとき該当属性を出力しず、ref のみ出すことを確認する。</summary>
    [Fact]
    public void Format_OmitsAttributesWhenAbsent()
    {
        var root = Leaf("Button", refId: "s1e2");
        var text = SnapshotTextFormatter.Format(Meta(), root, "operable");
        var line = text.Split('\n').First(l => l.TrimStart().StartsWith("- Button", StringComparison.Ordinal));
        Assert.DoesNotContain("[aid=", line, StringComparison.Ordinal);
        Assert.DoesNotContain("[value=", line, StringComparison.Ordinal);
        Assert.DoesNotContain("[disabled]", line, StringComparison.Ordinal);
        Assert.DoesNotContain("[focused]", line, StringComparison.Ordinal);
        Assert.Contains("[ref=s1e2]", line, StringComparison.Ordinal);
    }

    /// <summary>階層ツリーを 1 階層あたり 2 スペースでインデントして出力することを確認する。</summary>
    [Fact]
    public void Format_IndentsByDepthTwoSpaces()
    {
        var grand = Leaf("Button", "x", refId: "s1e3");
        var child = new SnapshotElement("Pane", "P", null, null, true, false, false, false, false, "s1e2", new[] { grand });
        var root = new SnapshotElement("Window", "T", null, null, true, false, false, false, false, "s1e1", new[] { child });
        var text = SnapshotTextFormatter.Format(Meta(), root, "operable");

        Assert.Contains("\n- Window \"T\" [ref=s1e1]\n", text);
        Assert.Contains("\n  - Pane \"P\" [ref=s1e2]\n", text);
        Assert.Contains("\n    - Button \"x\" [ref=s1e3]\n", text);
    }

    /// <summary>name にダブルクオートが含まれるとき \" としてエスケープされることを確認する。</summary>
    [Fact]
    public void Format_EscapesQuotesInName()
    {
        var root = Leaf("Button", "He said \"hi\"", refId: "s1e1");
        var text = SnapshotTextFormatter.Format(Meta(), root, "operable");
        Assert.Contains("- Button \"He said \\\"hi\\\"\" [ref=s1e1]", text, StringComparison.Ordinal);
    }

    /// <summary>モーダルダイアログとしてマークされたウィンドウに [modal] フラグが付与されることを確認する。</summary>
    [Fact]
    public void Format_ModalFlag_IsEmitted()
    {
        var root = Leaf("Window", "Save?", modal: true, refId: "s1e1");
        var text = SnapshotTextFormatter.Format(Meta(), root, "operable");
        Assert.Contains("[modal]", text, StringComparison.Ordinal);
    }

    /// <summary>選択状態の要素に [selected] フラグが付与されることを確認する。</summary>
    [Fact]
    public void Format_SelectedFlag_IsEmitted()
    {
        var root = Leaf("ListItem", "Item1", selected: true, refId: "s1e1");
        var text = SnapshotTextFormatter.Format(Meta(), root, "operable");
        Assert.Contains("[selected]", text, StringComparison.Ordinal);
    }

    /// <summary>processName に日本語を含むとき、frontmatter でダブルクオートで囲むことを確認する (YAML 互換性)。</summary>
    [Fact]
    public void Format_Frontmatter_QuotesProcessNameWithJapanese()
    {
        var root = Leaf("Window", "T", refId: "s1e1");
        var meta = new SnapshotMeta("s1", "電卓", 1234, "2025-01-01T00:00:00Z");
        var text = SnapshotTextFormatter.Format(meta, root, "operable");
        Assert.Contains("processName: \"電卓\"\n", text, StringComparison.Ordinal);
    }

    /// <summary>processName に ":" が含まれるときクォートされることを確認する (YAML ポイズング防止)。</summary>
    [Fact]
    public void Format_Frontmatter_QuotesProcessNameWithColon()
    {
        var root = Leaf("Window", "T", refId: "s1e1");
        var meta = new SnapshotMeta("s1", "foo:bar", 1, "2025-01-01T00:00:00Z");
        var text = SnapshotTextFormatter.Format(meta, root, "operable");
        Assert.Contains("processName: \"foo:bar\"\n", text, StringComparison.Ordinal);
    }

    /// <summary>generatedAt は常にダブルクオートで囲まれることを確認する (コロンを含む ISO 8601 のポイズング防止)。</summary>
    [Fact]
    public void Format_Frontmatter_AlwaysQuotesGeneratedAt()
    {
        var root = Leaf("Window", "T", refId: "s1e1");
        var meta = new SnapshotMeta("s1", "notepad", 1, "2026-04-28T03:42:20.900Z");
        var text = SnapshotTextFormatter.Format(meta, root, "operable");
        Assert.Contains("generatedAt: \"2026-04-28T03:42:20.900Z\"\n", text, StringComparison.Ordinal);
    }

    /// <summary>ASCII のシンプルな processName はクォートせずそのまま出力されることを確認する。</summary>
    [Fact]
    public void Format_Frontmatter_LeavesPlainProcessNameBare()
    {
        var root = Leaf("Window", "T", refId: "s1e1");
        var meta = new SnapshotMeta("s1", "notepad", 1, "2025-01-01T00:00:00Z");
        var text = SnapshotTextFormatter.Format(meta, root, "operable");
        Assert.Contains("\nprocessName: notepad\n", text, StringComparison.Ordinal);
        Assert.Contains("\nsessionId: s1\n", text, StringComparison.Ordinal);
        Assert.Contains("\nfilter: operable\n", text, StringComparison.Ordinal);
    }
}
