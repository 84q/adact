using Adact.Engine;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Click Options behavior.</summary>
[Trait("Layer", "Unit")]
public class ClickOptionsTests
{
    /// <summary>Performs the Defaults Are Left Single Click With No Modifiers And Center operation.</summary>
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

    /// <summary>Performs the Record Overrides All Fields operation.</summary>
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
