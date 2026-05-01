using Xunit;

namespace Adact.Mcp.Http.Tests;

/// <summary>
/// Verifies locale- and UWP-tolerant Calculator window detection used by HTTP E2E tests.
/// </summary>
[Trait("Layer", "Unit")]
public class CalculatorWindowFinderTests
{
    /// <summary>Calculator-like process names or titles resolve to the advertised windowRef.</summary>
    [Theory]
    [InlineData("[{\"windowRef\":\"w1\",\"processName\":\"CalculatorApp\",\"windowTitle\":\"\"}]", "w1")]
    [InlineData("[{\"windowRef\":\"w2\",\"processName\":\"ApplicationFrameHost\",\"windowTitle\":\"Calculator\"}]", "w2")]
    [InlineData("[{\"windowRef\":\"w3\",\"processName\":\"ApplicationFrameHost\",\"windowTitle\":\"電卓\"}]", "w3")]
    public void FindWindowRef_WhenCalculatorVisible_ReturnsWindowRef(string listText, string expected)
    {
        Assert.Equal(expected, CalculatorWindowFinder.FindWindowRef(listText));
    }

    /// <summary>Unrelated app windows are ignored.</summary>
    [Fact]
    public void FindWindowRef_WhenCalculatorMissing_ReturnsNull()
    {
        const string listText = "[{\"windowRef\":\"w1\",\"processName\":\"notepad\",\"windowTitle\":\"Untitled - Notepad\"}]";

        Assert.Null(CalculatorWindowFinder.FindWindowRef(listText));
    }
}
