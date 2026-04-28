using Adact.Cli.Snapshots;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class SnapshotTextFormatterTests
{
  private static SnapshotElement Leaf(
      string role, string? name = null, string? aid = null,
      string? value = null, bool isEnabled = true,
      bool focused = false, bool modal = false,
      string refId = "s1e1")
      => new(role, name, aid, Value: value,
          IsEnabled: isEnabled, IsOffscreen: false, HasKeyboardFocus: focused,
          IsModalDialog: modal, Ref: refId, Children: Array.Empty<SnapshotElement>());

  private static SnapshotMeta Meta() =>
      new("s1", "notepad", 1234, "2025-01-01T00:00:00Z");

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

  [Fact]
  public void Format_IndentsByDepthTwoSpaces()
  {
    var grand = Leaf("Button", "x", refId: "s1e3");
    var child = new SnapshotElement("Pane", "P", null, null, true, false, false, false, "s1e2", new[] { grand });
    var root = new SnapshotElement("Window", "T", null, null, true, false, false, false, "s1e1", new[] { child });
    var text = SnapshotTextFormatter.Format(Meta(), root, "operable");

    Assert.Contains("\n- Window \"T\" [ref=s1e1]\n", text);
    Assert.Contains("\n  - Pane \"P\" [ref=s1e2]\n", text);
    Assert.Contains("\n    - Button \"x\" [ref=s1e3]\n", text);
  }

  [Fact]
  public void Format_EscapesQuotesInName()
  {
    var root = Leaf("Button", "He said \"hi\"", refId: "s1e1");
    var text = SnapshotTextFormatter.Format(Meta(), root, "operable");
    Assert.Contains("- Button \"He said \\\"hi\\\"\" [ref=s1e1]", text, StringComparison.Ordinal);
  }

  [Fact]
  public void Format_ModalFlag_IsEmitted()
  {
    var root = Leaf("Window", "Save?", modal: true, refId: "s1e1");
    var text = SnapshotTextFormatter.Format(Meta(), root, "operable");
    Assert.Contains("[modal]", text, StringComparison.Ordinal);
  }

  [Fact]
  public void Format_Frontmatter_QuotesProcessNameWithJapanese()
  {
    var root = Leaf("Window", "T", refId: "s1e1");
    var meta = new SnapshotMeta("s1", "電卓", 1234, "2025-01-01T00:00:00Z");
    var text = SnapshotTextFormatter.Format(meta, root, "operable");
    Assert.Contains("processName: \"電卓\"\n", text, StringComparison.Ordinal);
  }

  [Fact]
  public void Format_Frontmatter_QuotesProcessNameWithColon()
  {
    var root = Leaf("Window", "T", refId: "s1e1");
    var meta = new SnapshotMeta("s1", "foo:bar", 1, "2025-01-01T00:00:00Z");
    var text = SnapshotTextFormatter.Format(meta, root, "operable");
    Assert.Contains("processName: \"foo:bar\"\n", text, StringComparison.Ordinal);
  }

  [Fact]
  public void Format_Frontmatter_AlwaysQuotesGeneratedAt()
  {
    var root = Leaf("Window", "T", refId: "s1e1");
    var meta = new SnapshotMeta("s1", "notepad", 1, "2026-04-28T03:42:20.900Z");
    var text = SnapshotTextFormatter.Format(meta, root, "operable");
    Assert.Contains("generatedAt: \"2026-04-28T03:42:20.900Z\"\n", text, StringComparison.Ordinal);
  }

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
