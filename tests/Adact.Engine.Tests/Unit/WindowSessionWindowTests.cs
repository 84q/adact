using Adact.Engine;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// <see cref="WindowSession.ResizeAsync(int, int, CancellationToken)"/> の引数検証 (Phase 8 Step 5) を
/// 検証する Unit テスト。実 UIA / FlaUI には依存せず、<see cref="WindowSession.CreateForTest(int, WindowInfo)"/> で
/// 生成した最小セッションに対して引数検証段階の例外のみを確認する。
/// </summary>
[Trait("Layer", "Unit")]
public class WindowSessionWindowTests
{
    private static WindowSession CreateSession()
    {
        var info = new WindowInfo(
            ProcessId: 12345,
            ProcessName: "fake",
            Title: "Fake",
            ControlType: "Window",
            ClassName: null,
            NativeWindowHandle: 0x1234);
        return WindowSession.CreateForTest(1, info);
    }

    /// <summary>
    /// width が 0 以下の場合、ResizeAsync は <see cref="ArgumentOutOfRangeException"/> を投げる
    /// (UIA への到達前に弾く) ことを確認する。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ResizeAsync_NonPositiveWidth_ThrowsArgumentOutOfRange(int width)
    {
        var session = CreateSession();
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => session.ResizeAsync(width, 100));
        Assert.Equal("width", ex.ParamName);
    }

    /// <summary>
    /// height が 0 以下の場合、ResizeAsync は <see cref="ArgumentOutOfRangeException"/> を投げることを確認する。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ResizeAsync_NonPositiveHeight_ThrowsArgumentOutOfRange(int height)
    {
        var session = CreateSession();
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => session.ResizeAsync(100, height));
        Assert.Equal("height", ex.ParamName);
    }

    /// <summary>
    /// width / height が共に 0 の場合、最初の引数 (width) で ArgumentOutOfRangeException が投げられることを確認する。
    /// </summary>
    [Fact]
    public async Task ResizeAsync_BothZero_ThrowsArgumentOutOfRangeForWidth()
    {
        var session = CreateSession();
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => session.ResizeAsync(0, 0));
        Assert.Equal("width", ex.ParamName);
    }
}
