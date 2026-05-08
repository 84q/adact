using Adact.Cli.Commands;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Ref Validator behavior.</summary>
[Trait("Layer", "Unit")]
public class RefValidatorTests
{
    /// <summary>Gets a value indicating whether Is Element Ref Cases.</summary>
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

    /// <summary>Gets a value indicating whether Is Element Ref Null Returns False.</summary>
    [Fact]
    public void IsElementRef_Null_ReturnsFalse()
    {
        Assert.False(RefValidator.IsElementRef(null));
    }

    /// <summary>Gets a value indicating whether Is Session Ref Cases.</summary>
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

    /// <summary>Gets a value indicating whether Is Session Ref Null Returns False.</summary>
    [Fact]
    public void IsSessionRef_Null_ReturnsFalse()
    {
        Assert.False(RefValidator.IsSessionRef(null));
    }

    /// <summary>Gets a value indicating whether Is Window Ref Cases.</summary>
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

    /// <summary>Gets a value indicating whether Is Window Ref Null Returns False.</summary>
    [Fact]
    public void IsWindowRef_Null_ReturnsFalse()
    {
        Assert.False(RefValidator.IsWindowRef(null));
    }

    /// <summary>Performs the Extract Session Id Element Ref Returns Session Id operation.</summary>
    [Theory]
    [InlineData("s10e7", "s10")]
    [InlineData("s1e2", "s1")]
    [InlineData("s0e0", "s0")]
    public void ExtractSessionId_ElementRef_ReturnsSessionId(string input, string expected)
    {
        Assert.Equal(expected, RefValidator.ExtractSessionId(input));
    }

    /// <summary>Performs the Extract Session Id Not Element Ref Returns Null operation.</summary>
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

    /// <summary>Performs the Extract Session Id Null Returns Null operation.</summary>
    [Fact]
    public void ExtractSessionId_Null_ReturnsNull()
    {
        Assert.Null(RefValidator.ExtractSessionId(null));
    }
}
