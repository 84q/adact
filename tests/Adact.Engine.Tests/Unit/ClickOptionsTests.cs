using Adact.Engine;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// <see cref="ClickOptions"/> のデフォルト値を検証する Unit テスト。
/// 既存呼び出し (<c>ClickAsync(refId, options:null)</c>) の互換性回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class ClickOptionsTests
{
    /// <summary>パラメータ無指定時のデフォルトが設計 022 §6 と一致する。</summary>
    [Fact]
    public void Defaults_AreLeftSingleClickWithNoModifiersAndCenter()
    {
        var opts = new ClickOptions();
        Assert.False(opts.Double);
        Assert.Equal(MouseButton.Left, opts.Button);
        Assert.Equal(1, opts.Count);
        Assert.Null(opts.Modifiers);
        Assert.Null(opts.PositionX);
        Assert.Null(opts.PositionY);
    }

    /// <summary>すべてのフィールドを上書きできる。</summary>
    [Fact]
    public void Record_OverridesAllFields()
    {
        var opts = new ClickOptions(
          Double: true,
          Button: MouseButton.Right,
          Count: 3,
          Modifiers: new[] { "Shift" },
          PositionX: 10,
          PositionY: 20);
        Assert.True(opts.Double);
        Assert.Equal(MouseButton.Right, opts.Button);
        Assert.Equal(3, opts.Count);
        Assert.Equal(10, opts.PositionX);
        Assert.Equal(20, opts.PositionY);
        Assert.NotNull(opts.Modifiers);
    }
}
