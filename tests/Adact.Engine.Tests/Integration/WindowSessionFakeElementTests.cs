using Adact.Engine;
using Adact.Engine.Exceptions;

using FlaUI.Core.WindowsAPI;

using Xunit;

namespace Adact.Engine.Tests.Integration;

/// <summary>
/// Verifies WindowSession success paths with in-memory IElement trees and no FlaUI dependency.
/// </summary>
[Trait("Layer", "Integration")]
public class WindowSessionFakeElementTests
{
    private sealed class FakeInteractionDriver : IWindowInteractionDriver
    {
        public List<string> Calls { get; } = [];
        public void FocusWindow() => Calls.Add("focus-window");
        public void TypeKey(VirtualKeyShort key) => Calls.Add($"type-key:{key}");
        public void TypeChar(char ch) => Calls.Add($"type-char:{ch}");
        public void TypeText(string text) => Calls.Add($"type-text:{text}");
        public void PressKey(VirtualKeyShort key) => Calls.Add($"press-key:{key}");
        public void ReleaseKey(VirtualKeyShort key) => Calls.Add($"release-key:{key}");
        public void MoveTo(int x, int y) => Calls.Add($"move:{x},{y}");
        public void MouseDown(MouseButton button) => Calls.Add($"mousedown:{button}");
        public void MouseUp(MouseButton button) => Calls.Add($"mouseup:{button}");
        public void MouseClick(MouseButton button) => Calls.Add($"mouseclick:{button}");
        public void MouseDoubleClick(MouseButton button) => Calls.Add($"mousedoubleclick:{button}");
        public void Scroll(int amount) => Calls.Add($"scroll:{amount}");
        public void HorizontalScroll(int amount) => Calls.Add($"hscroll:{amount}");
        public Task WaitAfterInteractionAsync(CancellationToken ct)
        {
            Calls.Add("wait");
            return Task.CompletedTask;
        }
    }

    private static WindowInfo Info()
        => new(
            ProcessId: 12345,
            ProcessName: "fake",
            Title: "Fake Window",
            ControlType: "Window",
            ClassName: null,
            NativeWindowHandle: 0x1234);

    private static WindowSession CreateSession(FakeElement root)
        => WindowSession.CreateForTest(1, Info(), root);

    private static WindowSession CreateSession(FakeElement root, FakeInteractionDriver driver)
        => WindowSession.CreateForTest(1, Info(), root, driver);

    /// <summary>
    /// Snapshot registers fake elements and ClickAsync resolves the generated ref.
    /// </summary>
    [Fact]
    public async Task ClickAsync_AfterSnapshot_InvokesFakeElementClick()
    {
        var button = FakeElement.Button("OK").WithRect(10, 20, 30, 40);
        var root = FakeElement.Window("Fake Window", button);
        var session = CreateSession(root);

        await session.SnapshotAsync();
        await session.ClickAsync("s1e2");

        Assert.Equal(1, button.ClickCount);
    }

    /// <summary>
    /// FillAsync resolves a fake edit element and writes the requested value.
    /// </summary>
    [Fact]
    public async Task FillAsync_AfterSnapshot_SetsFakeElementText()
    {
        var edit = FakeElement.Edit(automationId: "input");
        var root = FakeElement.Window("Fake Window", edit);
        var session = CreateSession(root);

        await session.SnapshotAsync();
        await session.FillAsync("s1e2", "hello");

        Assert.Equal("hello", edit.LastFilledText);
    }

    /// <summary>
    /// WaitForRefAsync returns immediately when the fake element already satisfies the requested state.
    /// </summary>
    [Theory]
    [InlineData(false, true, WaitForState.Visible)]
    [InlineData(true, true, WaitForState.Hidden)]
    [InlineData(false, true, WaitForState.Enabled)]
    [InlineData(false, false, WaitForState.Disabled)]
    public async Task WaitForRefAsync_StateAlreadySatisfied_ReturnsResult(
        bool isOffscreen,
        bool isEnabled,
        WaitForState state)
    {
        var button = FakeElement.Button("OK");
        button.IsOffscreen = isOffscreen;
        button.IsEnabled = isEnabled;
        var root = FakeElement.Window("Fake Window", button);
        var session = CreateSession(root);

        await session.SnapshotAsync();
        var result = await session.WaitForRefAsync("s1e2", state, TimeSpan.FromSeconds(1));

        Assert.Equal("s1e2", result.Ref);
        Assert.Equal(state, result.State);
    }

    /// <summary>
    /// WaitForRefAsync reports detached when a previously valid ref no longer appears in a later snapshot.
    /// </summary>
    [Fact]
    public async Task WaitForRefAsync_DetachedAfterElementRemoved_ReturnsDetached()
    {
        var button = new FakeElement
        {
            ControlType = "Button",
            Name = "OK",
            RuntimeId = [10],
        };
        var root = FakeElement.Window("Fake Window", button);
        var session = CreateSession(root);

        await session.SnapshotAsync();
        root.ChildList.Clear();

        var result = await session.WaitForRefAsync("s1e2", WaitForState.Detached, TimeSpan.FromSeconds(1));

        Assert.Equal("s1e2", result.Ref);
        Assert.Equal(WaitForState.Detached, result.State);
    }

    /// <summary>
    /// WaitForQueryAsync finds matching fake elements by query fields.
    /// </summary>
    [Fact]
    public async Task WaitForQueryAsync_MatchingElement_ReturnsGeneratedRef()
    {
        var edit = FakeElement.Edit(automationId: "name-box");
        edit.Name = "Name";
        var root = FakeElement.Window("Fake Window", edit);
        var session = CreateSession(root);

        var result = await session.WaitForQueryAsync(
            new WaitForElementQuery("Name", "Edit", "name-box", null),
            WaitForState.Enabled,
            TimeSpan.FromSeconds(1));

        Assert.Equal("s1e2", result.Ref);
        Assert.Equal(WaitForState.Enabled, result.State);
    }

    /// <summary>
    /// WaitForQueryAsync throws WaitTimeoutException when no fake element matches.
    /// </summary>
    [Fact]
    public async Task WaitForQueryAsync_NoMatch_ThrowsWaitTimeoutException()
    {
        var root = FakeElement.Window("Fake Window", FakeElement.Button("OK"));
        var session = CreateSession(root);

        await Assert.ThrowsAsync<WaitTimeoutException>(() =>
            session.WaitForQueryAsync(
                new WaitForElementQuery("Missing", null, null, null),
                WaitForState.Visible,
                TimeSpan.FromMilliseconds(120)));
    }

    /// <summary>
    /// PressAsync focuses a fake element and sends key input through the injected driver.
    /// </summary>
    [Fact]
    public async Task PressAsync_WithRef_UsesFakeElementAndInputDriver()
    {
        var button = FakeElement.Button("OK");
        var root = FakeElement.Window("Fake Window", button);
        var driver = new FakeInteractionDriver();
        var session = CreateSession(root, driver);

        await session.SnapshotAsync();
        await session.PressAsync("Ctrl+A", "s1e2");

        Assert.Equal(1, button.FocusCount);
        Assert.Contains(driver.Calls, c => c.StartsWith("press-key:", StringComparison.Ordinal));
        Assert.Contains(driver.Calls, c => c.StartsWith("release-key:", StringComparison.Ordinal));
        Assert.Contains(driver.Calls, c => c.StartsWith("type-key:", StringComparison.Ordinal));
        Assert.Contains("wait", driver.Calls);
    }

    /// <summary>
    /// TypeAsync focuses a fake element and sends text through the injected driver.
    /// </summary>
    [Fact]
    public async Task TypeAsync_WithRef_UsesFakeElementAndInputDriver()
    {
        var edit = FakeElement.Edit();
        var root = FakeElement.Window("Fake Window", edit);
        var driver = new FakeInteractionDriver();
        var session = CreateSession(root, driver);

        await session.SnapshotAsync();
        await session.TypeAsync("s1e2", "abc");

        Assert.Equal(1, edit.FocusCount);
        Assert.Contains("type-text:abc", driver.Calls);
        Assert.Contains("wait", driver.Calls);
    }

    /// <summary>
    /// MouseMoveAsync resolves point and ref targets and uses the injected driver.
    /// </summary>
    [Fact]
    public async Task MouseMoveAsync_UsesInputDriver()
    {
        var button = FakeElement.Button("OK").WithRect(10, 20, 30, 40);
        var root = FakeElement.Window("Fake Window", button);
        var driver = new FakeInteractionDriver();
        var session = CreateSession(root, driver);

        await session.SnapshotAsync();
        await session.MouseMoveAsync(new MouseTarget.ByPoint(1, 2));
        await session.MouseMoveAsync(new MouseTarget.ByRef("s1e2"));

        Assert.Contains("move:1,2", driver.Calls);
        Assert.Contains("move:25,40", driver.Calls);
    }

    /// <summary>
    /// ClickWithOptionsAsync and DoubleClickAsync use the injected driver for physical clicks.
    /// </summary>
    [Fact]
    public async Task DetailedClickAsync_UsesInputDriver()
    {
        var button = FakeElement.Button("OK").WithRect(10, 20, 30, 40);
        var root = FakeElement.Window("Fake Window", button);
        var driver = new FakeInteractionDriver();
        var session = CreateSession(root, driver);

        await session.SnapshotAsync();
        await session.ClickWithOptionsAsync("s1e2", new ClickOptions(Button: MouseButton.Right, Count: 2, PositionX: 1, PositionY: 2));
        await session.DoubleClickAsync("s1e2", new ClickOptions(Button: MouseButton.Middle));

        Assert.Contains("focus-window", driver.Calls);
        Assert.Contains("move:11,22", driver.Calls);
        Assert.Equal(2, driver.Calls.Count(c => c == "mouseclick:Right"));
        Assert.Contains("mousedoubleclick:Middle", driver.Calls);
    }

    /// <summary>
    /// Mouse button and wheel operations use the injected driver.
    /// </summary>
    [Fact]
    public async Task MouseButtonAndWheelAsync_UseInputDriver()
    {
        var root = FakeElement.Window("Fake Window");
        var driver = new FakeInteractionDriver();
        var session = CreateSession(root, driver);

        await session.MouseDownAsync(new MouseTarget.ByPoint(5, 6), MouseButton.Left);
        await session.MouseUpAsync(new MouseTarget.ByPoint(5, 6), MouseButton.Left);
        await session.MouseWheelAsync(new MouseTarget.ByPoint(5, 6), deltaX: 2, deltaY: 3);

        Assert.Contains("mousedown:Left", driver.Calls);
        Assert.Contains("mouseup:Left", driver.Calls);
        Assert.Contains("scroll:-3", driver.Calls);
        Assert.Contains("hscroll:2", driver.Calls);
    }

    /// <summary>
    /// CheckAsync and UncheckAsync use fake checkable element capability.
    /// </summary>
    [Fact]
    public async Task CheckAndUncheckAsync_UseFakeCheckableCapability()
    {
        var checkbox = FakeElement.Button("Accept");
        var root = FakeElement.Window("Fake Window", checkbox);
        var session = CreateSession(root);

        await session.SnapshotAsync();
        await session.CheckAsync("s1e2");
        Assert.True(checkbox.LastSetChecked);

        await session.UncheckAsync("s1e2");
        Assert.False(checkbox.LastSetChecked);
    }

    /// <summary>
    /// SelectAsync uses fake selectable element capability for name, index, and item-ref selectors.
    /// </summary>
    [Fact]
    public async Task SelectAsync_UsesFakeSelectableCapability()
    {
        var item = FakeElement.Button("Item");
        var list = FakeElement.Pane("List", item);
        var root = FakeElement.Window("Fake Window", list);
        var session = CreateSession(root);

        await session.SnapshotAsync();
        await session.SelectAsync("s1e2", [SelectionTarget.FromName("Item")]);
        Assert.Equal("Item", list.LastSelectedName);

        await session.SelectAsync("s1e2", [SelectionTarget.FromIndex(0)]);
        Assert.Equal(0, list.LastSelectedIndex);

        await session.SelectAsync("s1e2", [SelectionTarget.FromItemRef("s1e3")]);
        Assert.NotNull(list.LastSelectedTargets);
        Assert.IsType<SelectionTarget.ByItemRef>(list.LastSelectedTargets[0]);
    }

    /// <summary>
    /// ScrollIntoViewAsync uses fake scrollable element capability.
    /// </summary>
    [Fact]
    public async Task ScrollIntoViewAsync_UsesFakeScrollableCapability()
    {
        var item = FakeElement.Button("Item");
        var root = FakeElement.Window("Fake Window", item);
        var session = CreateSession(root);

        await session.SnapshotAsync();
        await session.ScrollIntoViewAsync("s1e2");

        Assert.Equal(1, item.ScrollIntoViewCount);
    }
}

file static class FakeElementTestExtensions
{
    public static FakeElement WithRect(this FakeElement element, int x, int y, int width, int height)
    {
        element.BoundingRectangle = new Rect(x, y, width, height);
        return element;
    }
}
