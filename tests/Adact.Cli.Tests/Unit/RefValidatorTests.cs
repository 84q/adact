using Adact.Cli.Commands;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class RefValidatorTests
{
    [Theory]
    [InlineData("s1g2e3", true)]
    [InlineData("s10g3e7", true)]
    [InlineData("s0g0e0", true)]
    [InlineData("foo", false)]
    [InlineData("s1", false)]
    [InlineData("w1", false)]
    [InlineData("s1g2", false)]
    [InlineData("s1g2e", false)]
    [InlineData("S1G2E3", false)]
    [InlineData("", false)]
    public void IsElementRef_Cases(string input, bool expected)
    {
        Assert.Equal(expected, RefValidator.IsElementRef(input));
    }

    [Fact]
    public void IsElementRef_Null_ReturnsFalse()
    {
        Assert.False(RefValidator.IsElementRef(null));
    }

    [Theory]
    [InlineData("s1", true)]
    [InlineData("s10", true)]
    [InlineData("s1g2e3", false)]
    [InlineData("w1", false)]
    [InlineData("foo", false)]
    [InlineData("", false)]
    public void IsSessionRef_Cases(string input, bool expected)
    {
        Assert.Equal(expected, RefValidator.IsSessionRef(input));
    }

    [Fact]
    public void IsSessionRef_Null_ReturnsFalse()
    {
        Assert.False(RefValidator.IsSessionRef(null));
    }

    [Theory]
    [InlineData("w1", true)]
    [InlineData("w10", true)]
    [InlineData("s1", false)]
    [InlineData("s1g2e3", false)]
    [InlineData("foo", false)]
    [InlineData("", false)]
    public void IsWindowRef_Cases(string input, bool expected)
    {
        Assert.Equal(expected, RefValidator.IsWindowRef(input));
    }

    [Fact]
    public void IsWindowRef_Null_ReturnsFalse()
    {
        Assert.False(RefValidator.IsWindowRef(null));
    }

    [Theory]
    [InlineData("s10g3e7", "s10")]
    [InlineData("s1g2e3", "s1")]
    [InlineData("s0g0e0", "s0")]
    public void ExtractSessionId_ElementRef_ReturnsSessionId(string input, string expected)
    {
        Assert.Equal(expected, RefValidator.ExtractSessionId(input));
    }

    [Theory]
    [InlineData("s1")]
    [InlineData("w1")]
    [InlineData("foo")]
    [InlineData("")]
    public void ExtractSessionId_NotElementRef_ReturnsNull(string input)
    {
        Assert.Null(RefValidator.ExtractSessionId(input));
    }

    [Fact]
    public void ExtractSessionId_Null_ReturnsNull()
    {
        Assert.Null(RefValidator.ExtractSessionId(null));
    }
}
