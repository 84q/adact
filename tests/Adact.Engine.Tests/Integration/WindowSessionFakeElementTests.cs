using Adact.Engine;
using Adact.Engine.Exceptions;

using FlaUI.Core.WindowsAPI;

using Xunit;

namespace Adact.Engine.Tests.Integration;

/// <summary>Contains tests for the Window Session Fake Element behavior.</summary>
[Trait("Layer", "Integration")]
public class WindowSessionFakeElementTests
{
    private sealed class FakeInteractionDriver : IWindowInteractionDriver
    {
        /// <summary>Gets the Calls value.</summary>
        public List<string> Calls { get; } = [];
        /// <summary>Performs the Focus Window operation.</summary>
        public void FocusWindow() => Calls.Add("focus-window");
        /// <summary>Performs the Type Key operation.</summary>
        public void TypeKey(VirtualKeyShort key) => Calls.Add($"type-key:{key}");
        /// <summary>Performs the Type Char operation.</summary>
        public void TypeChar(char ch) => Calls.Add($"type-char:{ch}");
        /// <summary>Performs the Type Text operation.</summary>
        public void TypeText(string text) => Calls.Add($"type-text:{text}");
        /// <summary>Performs the Press Key operation.</summary>
        public void PressKey(VirtualKeyShort key) => Calls.Add($"press-key:{key}");
        /// <summary>Performs the Release Key operation.</summary>
        public void ReleaseKey(VirtualKeyShort key) => Calls.Add($"release-key:{key}");
        /// <summary>Performs the Move To operation.</summary>
        public void MoveTo(int x, int y) => Calls.Add($"move:{x},{y}");
        /// <summary>Performs the Mouse Down operation.</summary>
        public void MouseDown(MouseButton button) => Calls.Add($"mousedown:{button}");
        /// <summary>Performs the Mouse Up operation.</summary>
        public void MouseUp(MouseButton button) => Calls.Add($"mouseup:{button}");
        /// <summary>Performs the Mouse Click operation.</summary>
        public void MouseClick(MouseButton button) => Calls.Add($"mouseclick:{button}");
        /// <summary>Performs the Mouse Double Click operation.</summary>
        public void MouseDoubleClick(MouseButton button) => Calls.Add($"mousedoubleclick:{button}");
        /// <summary>Performs the Scroll operation.</summary>
        public void Scroll(int amount) => Calls.Add($"scroll:{amount}");
        /// <summary>Performs the Horizontal Scroll operation.</summary>
        public void HorizontalScroll(int amount) => Calls.Add($"hscroll:{amount}");
        /// <summary>Waits for the Wait After Interaction Async condition.</summary>
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

    /// <summary>Performs the Click Async After Snapshot Invokes Fake Element Click operation.</summary>
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

    /// <summary>Performs the Fill Async After Snapshot Sets Fake Element Text operation.</summary>
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

    /// <summary>Waits for the Wait For Ref Async State Already Satisfied Returns Result condition.</summary>
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

    /// <summary>Waits for the Wait For Ref Async Detached After Element Removed Returns Detached condition.</summary>
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

    /// <summary>Waits for the Wait For Query Async Matching Element Returns Generated Ref condition.</summary>
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

    /// <summary>Waits for the Wait For Query Async No Match Throws Wait Timeout Exception condition.</summary>
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

    /// <summary>Performs the Press Async With Ref Uses Fake Element And Input Driver operation.</summary>
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

    /// <summary>Performs the Type Async With Ref Uses Fake Element And Input Driver operation.</summary>
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

    /// <summary>Performs the Mouse Move Async Uses Input Driver operation.</summary>
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

    /// <summary>Performs the Detailed Click Async Uses Input Driver operation.</summary>
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

    /// <summary>Performs the Mouse Button And Wheel Async Use Input Driver operation.</summary>
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

    /// <summary>Performs the Check And Uncheck Async Use Fake Checkable Capability operation.</summary>
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

    /// <summary>Performs the Select Async Uses Fake Selectable Capability operation.</summary>
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

    /// <summary>Performs the Scroll Into View Async Uses Fake Scrollable Capability operation.</summary>
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
    /// <summary>Performs the With Rect operation.</summary>
    public static FakeElement WithRect(this FakeElement element, int x, int y, int width, int height)
    {
        element.BoundingRectangle = new Rect(x, y, width, height);
        return element;
    }
}
