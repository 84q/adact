using Adact.Engine;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// Verifies WindowSession action argument validation that runs before UIA access.
/// </summary>
[Trait("Layer", "Unit")]
public class WindowSessionActionValidationTests
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
    /// Keyboard action methods reject null key or text before touching UIA.
    /// </summary>
    [Fact]
    public async Task KeyboardActions_NullInput_ThrowArgumentNullException()
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<ArgumentNullException>(() => session.PressAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.KeyDownAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.KeyUpAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.TypeAsync(refId: null, text: null!));
    }

    /// <summary>
    /// Key down/up only accept a single key, not key combinations.
    /// </summary>
    [Theory]
    [InlineData("Ctrl+A")]
    [InlineData("")]
    public async Task KeyDownAndKeyUp_InvalidSingleKey_ThrowArgumentException(string key)
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<ArgumentException>(() => session.KeyDownAsync(key));
        await Assert.ThrowsAsync<ArgumentException>(() => session.KeyUpAsync(key));
    }

    /// <summary>
    /// Mouse action methods reject null targets before touching UIA.
    /// </summary>
    [Fact]
    public async Task MouseActions_NullTarget_ThrowArgumentNullException()
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<ArgumentNullException>(() => session.MouseMoveAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.MouseDownAsync(null!, MouseButton.Left));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.MouseUpAsync(null!, MouseButton.Left));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.MouseWheelAsync(null!, deltaX: 0, deltaY: 1));
    }

    /// <summary>
    /// Detailed click rejects a null options object.
    /// </summary>
    [Fact]
    public async Task ClickWithOptions_NullOptions_ThrowsArgumentNullException()
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<ArgumentNullException>(() => session.ClickWithOptionsAsync("s1e1", null!));
    }

    /// <summary>
    /// Select requires exactly one selector among name, index, and itemRef.
    /// </summary>
    [Fact]
    public async Task Select_InvalidSelectorCount_ThrowsArgumentException()
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            session.SelectAsync("s1e1", name: null, index: null, itemRef: null));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            session.SelectAsync("s1e1", name: "Item", index: 0, itemRef: null));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            session.SelectAsync("s1e1", name: "Item", index: null, itemRef: "s1e2"));
    }

    /// <summary>
    /// Disposed sessions reject actions before attempting UIA interaction.
    /// </summary>
    [Fact]
    public async Task Actions_AfterDispose_ThrowObjectDisposedException()
    {
        var session = CreateSession();
        session.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.PressAsync("Enter"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.TypeAsync(null, "text"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.MouseMoveAsync(new MouseTarget.ByPoint(1, 2)));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.SelectAsync("s1e1", "Item", null, null));
    }
}
