using Xunit;

namespace Adact.Mcp.Http.Tests;

/// <summary>Contains tests for the Sample App Window Finder behavior.</summary>
[Trait("Layer", "Unit")]
public class SampleAppWindowFinderTests
{
    /// <summary>Performs the Find Window Ref When Sample App Visible Returns Window Ref operation.</summary>
    [Theory]
    [InlineData("[{\"windowRef\":\"w1\",\"processName\":\"SampleApp\",\"windowTitle\":\"\"}]", "w1")]
    [InlineData("[{\"windowRef\":\"w2\",\"processName\":\"ApplicationFrameHost\",\"windowTitle\":\"ADACT SampleApp\"}]", "w2")]
    public void FindWindowRef_WhenSampleAppVisible_ReturnsWindowRef(string listText, string expected)
    {
        Assert.Equal(expected, SampleAppWindowFinder.FindWindowRef(listText));
    }

    /// <summary>Performs the Find Window Ref When Sample App Missing Returns Null operation.</summary>
    [Fact]
    public void FindWindowRef_WhenSampleAppMissing_ReturnsNull()
    {
        const string listText = "[{\"windowRef\":\"w1\",\"processName\":\"notepad\",\"windowTitle\":\"Untitled - Notepad\"}]";

        Assert.Null(SampleAppWindowFinder.FindWindowRef(listText));
    }
}
