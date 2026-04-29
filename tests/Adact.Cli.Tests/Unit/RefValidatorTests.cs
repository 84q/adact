using Adact.Cli.Commands;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>
/// <see cref="RefValidator"/> の ref パターン判定 (element/session/window) と sessionId 抽出を検証する Unit テスト。
/// ref-ids.md のフォーマット仕様の回帰防止。
/// </summary>
[Trait("Layer", "Unit")]
public class RefValidatorTests
{
    /// <summary>IsElementRef が s&lt;n&gt;e&lt;n&gt; パターンを true、それ以外 (foo / s1 / w1 / S1E2 ・空等) を false とすることを確認する。</summary>
    /// <param name="input">検証入力。</param>
    /// <param name="expected">期待値。</param>
    [Theory]
    [InlineData("s1e2", true)]
    [InlineData("s10e7", true)]
    [InlineData("s0e0", true)]
    [InlineData("foo", false)]
    [InlineData("s1", false)]
    [InlineData("w1", false)]
    [InlineData("s1e", false)]
    [InlineData("s1g2e3", false)]
    [InlineData("S1E2", false)]
    [InlineData("", false)]
    public void IsElementRef_Cases(string input, bool expected)
    {
        Assert.Equal(expected, RefValidator.IsElementRef(input));
    }

    /// <summary>IsElementRef(null) が false を返すことを確認する。null 引数で例外を投げず false を返す契約の回帰防止。</summary>
    [Fact]
    public void IsElementRef_Null_ReturnsFalse()
    {
        Assert.False(RefValidator.IsElementRef(null));
    }

    /// <summary>IsSessionRef は s&lt;n&gt; パターン ("s1", "s10") のみ true とし、s1e2 や w1 は false とすることを確認する。</summary>
    /// <param name="input">検証入力。</param>
    /// <param name="expected">期待値。</param>
    [Theory]
    [InlineData("s1", true)]
    [InlineData("s10", true)]
    [InlineData("s1e2", false)]
    [InlineData("w1", false)]
    [InlineData("foo", false)]
    [InlineData("", false)]
    public void IsSessionRef_Cases(string input, bool expected)
    {
        Assert.Equal(expected, RefValidator.IsSessionRef(input));
    }

    /// <summary>IsSessionRef(null) が false を返すことを確認する。null 引数で例外を投げず false を返す契約の回帰防止。</summary>
    [Fact]
    public void IsSessionRef_Null_ReturnsFalse()
    {
        Assert.False(RefValidator.IsSessionRef(null));
    }

    /// <summary>IsWindowRef は w&lt;n&gt; のみ true 、s* や その他は false とすることを確認する。</summary>
    /// <param name="input">検証入力。</param>
    /// <param name="expected">期待値。</param>
    [Theory]
    [InlineData("w1", true)]
    [InlineData("w10", true)]
    [InlineData("s1", false)]
    [InlineData("s1e2", false)]
    [InlineData("foo", false)]
    [InlineData("", false)]
    public void IsWindowRef_Cases(string input, bool expected)
    {
        Assert.Equal(expected, RefValidator.IsWindowRef(input));
    }

    /// <summary>IsWindowRef(null) が false を返すことを確認する。null 引数で例外を投げず false を返す契約の回帰防止。</summary>
    [Fact]
    public void IsWindowRef_Null_ReturnsFalse()
    {
        Assert.False(RefValidator.IsWindowRef(null));
    }

    /// <summary>ExtractSessionId が element ref から session 部分 ("s10" 等) を抽出することを確認する。</summary>
    /// <param name="input">検証入力 ref。</param>
    /// <param name="expected">期待される session ref。</param>
    [Theory]
    [InlineData("s10e7", "s10")]
    [InlineData("s1e2", "s1")]
    [InlineData("s0e0", "s0")]
    public void ExtractSessionId_ElementRef_ReturnsSessionId(string input, string expected)
    {
        Assert.Equal(expected, RefValidator.ExtractSessionId(input));
    }

    /// <summary>element ref ではない入力 (s1 単体・w1・foo 等) は null を返すことを確認する。</summary>
    /// <param name="input">検証入力 ref。</param>
    [Theory]
    [InlineData("s1")]
    [InlineData("w1")]
    [InlineData("foo")]
    [InlineData("s1g2e3")]
    [InlineData("")]
    public void ExtractSessionId_NotElementRef_ReturnsNull(string input)
    {
        Assert.Null(RefValidator.ExtractSessionId(input));
    }

    /// <summary>ExtractSessionId(null) が null を返すことを確認する。</summary>
    [Fact]
    public void ExtractSessionId_Null_ReturnsNull()
    {
        Assert.Null(RefValidator.ExtractSessionId(null));
    }
}
