using Adact.Engine;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Window Session Action Validation behavior.</summary>
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

    /// <summary>Performs the Keyboard Actions Null Input Throw Argument Null Exception operation.</summary>
    [Fact]
    public async Task KeyboardActions_NullInput_ThrowArgumentNullException()
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<ArgumentNullException>(() => session.PressAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.KeyDownAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.KeyUpAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.TypeAsync(refId: null, text: null!));
    }

    /// <summary>Performs the Key Down And Key Up Invalid Single Key Throw Argument Exception operation.</summary>
    [Theory]
    [InlineData("Ctrl+A")]
    [InlineData("")]
    public async Task KeyDownAndKeyUp_InvalidSingleKey_ThrowArgumentException(string key)
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<ArgumentException>(() => session.KeyDownAsync(key));
        await Assert.ThrowsAsync<ArgumentException>(() => session.KeyUpAsync(key));
    }

    /// <summary>Performs the Mouse Actions Null Target Throw Argument Null Exception operation.</summary>
    [Fact]
    public async Task MouseActions_NullTarget_ThrowArgumentNullException()
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<ArgumentNullException>(() => session.MouseMoveAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.MouseDownAsync(null!, MouseButton.Left));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.MouseUpAsync(null!, MouseButton.Left));
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.MouseWheelAsync(null!, deltaX: 0, deltaY: 1));
    }

    /// <summary>Performs the Click With Options Null Options Throws Argument Null Exception operation.</summary>
    [Fact]
    public async Task ClickWithOptions_NullOptions_ThrowsArgumentNullException()
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<ArgumentNullException>(() => session.ClickWithOptionsAsync("s1e1", null!));
    }

    /// <summary>Performs the Select Empty Targets Throws Argument Exception operation.</summary>
    [Fact]
    public async Task Select_EmptyTargets_ThrowsArgumentException()
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            session.SelectAsync("s1e1", []));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            session.SelectAsync("s1e1", null!));
    }

    /// <summary>Performs the Actions After Dispose Throw Object Disposed Exception operation.</summary>
    [Fact]
    public async Task Actions_AfterDispose_ThrowObjectDisposedException()
    {
        var session = CreateSession();
        session.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.PressAsync("Enter"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.TypeAsync(null, "text"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.MouseMoveAsync(new MouseTarget.ByPoint(1, 2)));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.SelectAsync("s1e1", [SelectionTarget.FromName("Item")]));
    }
}
