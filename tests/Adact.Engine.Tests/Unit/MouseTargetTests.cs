using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// <see cref="MouseTarget.Parse"/> の入力解析を検証する Unit テスト。
/// CLI / MCP の共通 target 引数文法 (Phase 8 設計 §4) の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class MouseTargetTests
{
    /// <summary>ref 形式 (<c>s1e2</c>) は <see cref="MouseTarget.ByRef"/> に解析される。</summary>
    [Fact]
    public void Parse_RefSyntax_ReturnsByRef()
    {
        var t = MouseTarget.Parse("s1e2");
        var byRef = Assert.IsType<MouseTarget.ByRef>(t);
        Assert.Equal("s1e2", byRef.Ref);
    }

    /// <summary>正の座標 (<c>20,30</c>) は <see cref="MouseTarget.ByPoint"/> に解析される。</summary>
    [Fact]
    public void Parse_PositivePoint_ReturnsByPoint()
    {
        var t = MouseTarget.Parse("20,30");
        var p = Assert.IsType<MouseTarget.ByPoint>(t);
        Assert.Equal(20, p.X);
        Assert.Equal(30, p.Y);
    }

    /// <summary>負値座標 (<c>-100,-50</c>) もマルチモニタ対応として受理される。</summary>
    [Fact]
    public void Parse_NegativePoint_ReturnsByPoint()
    {
        var t = MouseTarget.Parse("-100,-50");
        var p = Assert.IsType<MouseTarget.ByPoint>(t);
        Assert.Equal(-100, p.X);
        Assert.Equal(-50, p.Y);
    }

    /// <summary>原点 (<c>0,0</c>) も有効な座標として受理される。</summary>
    [Fact]
    public void Parse_Origin_ReturnsByPoint()
    {
        var t = MouseTarget.Parse("0,0");
        var p = Assert.IsType<MouseTarget.ByPoint>(t);
        Assert.Equal(0, p.X);
        Assert.Equal(0, p.Y);
    }

    /// <summary>null / 空文字 / 不正フォーマットはすべて <see cref="ArgumentException"/> をスローする。</summary>
    /// <param name="input">入力文字列 (空文字または不正な形式)。</param>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("s1")]
    [InlineData("e2")]
    [InlineData("1,2,3")]
    [InlineData("1.5,2.5")]
    [InlineData("S1E2")]      // 大文字は ref 形式として認めない (regex は case-sensitive)
    [InlineData(" s1e2 ")]    // 前後空白は不可
    [InlineData("1,2,")]      // 末尾カンマ
    [InlineData(",1,2")]      // 先頭カンマ
    [InlineData("１,２")]    // 全角数字は不可
    [InlineData("9999999999,0")] // int 上限超過 → ArgumentException で統一
    public void Parse_InvalidInput_Throws(string input)
    {
        Assert.Throws<ArgumentException>(() => MouseTarget.Parse(input));
    }

    /// <summary>null 入力は <see cref="ArgumentException"/> をスローする。</summary>
    [Fact]
    public void Parse_Null_Throws()
    {
        Assert.Throws<ArgumentException>(() => MouseTarget.Parse(null!));
    }

    /// <summary><see cref="int.MaxValue"/> / <see cref="int.MinValue"/> も正常な座標として受理する。</summary>
    [Fact]
    public void Parse_IntBounds_ReturnsByPoint()
    {
        var t = MouseTarget.Parse($"{int.MaxValue},{int.MinValue}");
        var p = Assert.IsType<MouseTarget.ByPoint>(t);
        Assert.Equal(int.MaxValue, p.X);
        Assert.Equal(int.MinValue, p.Y);
    }
}
