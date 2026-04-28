using Adact.Cli.Snapshots;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class SnapshotTreeFilterTests
{
  private static SnapshotElement E(
      string role,
      string? name = null,
      string? aid = null,
      bool isOffscreen = false,
      params SnapshotElement[] children)
      => new(role, name, aid, Value: null,
          IsEnabled: true, IsOffscreen: isOffscreen, HasKeyboardFocus: false,
          IsModalDialog: false, Ref: $"s1e{role}{name}", Children: children);

  [Fact]
  public void Apply_Raw_KeepsTreeIntact()
  {
    var root = E("Window", "T", children: new[]
    {
      E("Pane", null, children: new[] { E("Button", "x") }),
    });
    var result = SnapshotTreeFilter.Apply(root, "raw");
    Assert.Equal("Pane", result.Children[0].Role);
  }

  [Fact]
  public void Apply_Operable_FlattensUnnamedPane()
  {
    var root = E("Window", "T", children: new[]
    {
      E("Pane", null, children: new[] { E("Button", "x") }),
    });
    var result = SnapshotTreeFilter.Apply(root, "operable");
    Assert.Single(result.Children);
    Assert.Equal("Button", result.Children[0].Role);
  }

  [Fact]
  public void Apply_Operable_KeepsNamedPane()
  {
    var root = E("Window", "T", children: new[]
    {
      E("Pane", "Named", children: new[] { E("Button", "inner") }),
    });
    var result = SnapshotTreeFilter.Apply(root, "operable");
    Assert.Single(result.Children);
    Assert.Equal("Pane", result.Children[0].Role);
    Assert.Equal("inner", result.Children[0].Children[0].Name);
  }

  [Fact]
  public void Apply_Operable_KeepsPaneWithAutomationId()
  {
    var root = E("Window", "T", children: new[]
    {
      E("Pane", null, aid: "main", children: new[] { E("Button", "inner") }),
    });
    var result = SnapshotTreeFilter.Apply(root, "operable");
    Assert.Single(result.Children);
    Assert.Equal("Pane", result.Children[0].Role);
  }

  [Fact]
  public void Apply_Operable_ExcludesOffscreen()
  {
    var hidden = E("Button", "hidden", isOffscreen: true);
    var visible = E("Button", "visible");
    var root = E("Window", "T", children: new[] { hidden, visible });
    var result = SnapshotTreeFilter.Apply(root, "operable");
    Assert.Single(result.Children);
    Assert.Equal("visible", result.Children[0].Name);
  }

  [Fact]
  public void Apply_Operable_FlattensUnknownControlType()
  {
    var root = E("Window", "T", children: new[]
    {
      E("WeirdRole", null, children: new[] { E("Button", "x") }),
    });
    var result = SnapshotTreeFilter.Apply(root, "operable");
    Assert.Single(result.Children);
    Assert.Equal("Button", result.Children[0].Role);
  }

  [Fact]
  public void Apply_Operable_NestedUnnamedPanes_AreFlattened()
  {
    var root = E("Window", "T", children: new[]
    {
      E("Pane", null, children: new[]
      {
        E("Pane", null, children: new[] { E("Button", "deep") }),
      }),
    });
    var result = SnapshotTreeFilter.Apply(root, "operable");
    Assert.Single(result.Children);
    Assert.Equal("Button", result.Children[0].Role);
  }

  [Fact]
  public void IsKnownFilter_AcceptsBothCases()
  {
    Assert.True(SnapshotTreeFilter.IsKnownFilter("operable"));
    Assert.True(SnapshotTreeFilter.IsKnownFilter("RAW"));
    Assert.False(SnapshotTreeFilter.IsKnownFilter("foo"));
  }
}
