using Xunit;

namespace Adact.Mcp.Http.Tests;

/// <summary>
/// SampleApp の window 検出ロジックを検証するユニットテスト。
/// </summary>
[Trait("Layer", "Unit")]
public class SampleAppWindowFinderTests
{
    /// <summary>SampleApp の processName または windowTitle から正しい windowRef が返ることを確認する。</summary>
    [Theory]
    [InlineData("[{\"windowRef\":\"w1\",\"processName\":\"SampleApp\",\"windowTitle\":\"\"}]", "w1")]
    [InlineData("[{\"windowRef\":\"w2\",\"processName\":\"ApplicationFrameHost\",\"windowTitle\":\"ADACT SampleApp\"}]", "w2")]
    public void FindWindowRef_WhenSampleAppVisible_ReturnsWindowRef(string listText, string expected)
    {
        Assert.Equal(expected, SampleAppWindowFinder.FindWindowRef(listText));
    }

    /// <summary>無関係なアプリのウィンドウは無視されることを確認する。</summary>
    [Fact]
    public void FindWindowRef_WhenSampleAppMissing_ReturnsNull()
    {
        const string listText = "[{\"windowRef\":\"w1\",\"processName\":\"notepad\",\"windowTitle\":\"Untitled - Notepad\"}]";

        Assert.Null(SampleAppWindowFinder.FindWindowRef(listText));
    }
}
