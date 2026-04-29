using Adact.Cli.Snapshots;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="SnapshotTreeFilter.Apply"/> の raw / operable フィルタ処理 (名無し Pane のフラット・offscreen 除外・未知 ControlType フラット等) を検証する Unit テスト。
/// snapshot.md §filter のツリー整形仕様の回帰防止。
/// </summary>
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

    /// <summary>raw フィルタではツリーを一切加工せずそのまま返すことを確認する。</summary>
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

    /// <summary>operable では name/aid を持たない Pane をフラットして子を持ち上げることを確認する。</summary>
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

    /// <summary>name を持つ Pane は operable でも保持されることを確認する。</summary>
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

    /// <summary>aid を持つ Pane は name が無くても operable で保持されることを確認する。</summary>
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

    /// <summary>isOffscreen=true の要素は operable で除外され、visible 要素のみ残ることを確認する。</summary>
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

    /// <summary>未知 ControlType の要素は operable でフラットされ、子要素が持ち上げられることを確認する (不要ノード除去の回帰防止)。</summary>
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

    /// <summary>多重にネストした名無し Pane も再帰的にフラットされることを確認する。</summary>
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

    /// <summary>IsKnownFilter が "operable"/"RAW" (大文字含む) に true、未知名に false を返すことを確認する。</summary>
    [Fact]
    public void IsKnownFilter_AcceptsBothCases()
    {
        Assert.True(SnapshotTreeFilter.IsKnownFilter("operable"));
        Assert.True(SnapshotTreeFilter.IsKnownFilter("RAW"));
        Assert.False(SnapshotTreeFilter.IsKnownFilter("foo"));
    }
}
