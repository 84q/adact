using Adact.Engine;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Mouse Target behavior.</summary>
[Trait("Layer", "Unit")]
public class MouseTargetTests
{
    /// <summary>Performs the Parse Ref Syntax Returns By Ref operation.</summary>
    [Fact]
    public void Parse_RefSyntax_ReturnsByRef()
    {
        var t = MouseTarget.Parse("s1e2");
        var byRef = Assert.IsType<MouseTarget.ByRef>(t);
        Assert.Equal("s1e2", byRef.Ref);
    }

    /// <summary>Performs the Parse Positive Point Returns By Point operation.</summary>
    [Fact]
    public void Parse_PositivePoint_ReturnsByPoint()
    {
        var t = MouseTarget.Parse("20,30");
        var p = Assert.IsType<MouseTarget.ByPoint>(t);
        Assert.Equal(20, p.X);
        Assert.Equal(30, p.Y);
    }

    /// <summary>Performs the Parse Negative Point Returns By Point operation.</summary>
    [Fact]
    public void Parse_NegativePoint_ReturnsByPoint()
    {
        var t = MouseTarget.Parse("-100,-50");
        var p = Assert.IsType<MouseTarget.ByPoint>(t);
        Assert.Equal(-100, p.X);
        Assert.Equal(-50, p.Y);
    }

    /// <summary>Performs the Parse Origin Returns By Point operation.</summary>
    [Fact]
    public void Parse_Origin_ReturnsByPoint()
    {
        var t = MouseTarget.Parse("0,0");
        var p = Assert.IsType<MouseTarget.ByPoint>(t);
        Assert.Equal(0, p.X);
        Assert.Equal(0, p.Y);
    }

    /// <summary>Performs the Parse Invalid Input Throws operation.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("s1")]
    [InlineData("e2")]
    [InlineData("1,2,3")]
    [InlineData("1.5,2.5")]
    public void Parse_InvalidInput_Throws(string input)
    {
        Assert.Throws<ArgumentException>(() => MouseTarget.Parse(input));
    }

    /// <summary>Performs the Parse Null Throws operation.</summary>
    [Fact]
    public void Parse_Null_Throws()
    {
        Assert.Throws<ArgumentException>(() => MouseTarget.Parse(null!));
    }

    /// <summary>Performs the Parse Int Bounds Returns By Point operation.</summary>
    [Fact]
    public void Parse_IntBounds_ReturnsByPoint()
    {
        var t = MouseTarget.Parse($"{int.MaxValue},{int.MinValue}");
        var p = Assert.IsType<MouseTarget.ByPoint>(t);
        Assert.Equal(int.MaxValue, p.X);
        Assert.Equal(int.MinValue, p.Y);
    }
}
