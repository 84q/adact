using Adact.Engine;
using Adact.Engine.Exceptions;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Window Session Wait For behavior.</summary>
[Trait("Layer", "Unit")]
public class WindowSessionWaitForTests
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

    private static WindowSession CreateSessionWithRoot(FakeElement root)
    {
        var info = new WindowInfo(
            ProcessId: 12345,
            ProcessName: "fake",
            Title: "Fake",
            ControlType: "Window",
            ClassName: null,
            NativeWindowHandle: 0x1234);
        return WindowSession.CreateForTest(1, info, root);
    }

    /// <summary>Waits for the Wait For Ref Async Null Ref Throws Argument Null Exception condition.</summary>
    [Fact]
    public async Task WaitForRefAsync_NullRef_ThrowsArgumentNullException()
    {
        var session = CreateSession();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            session.WaitForRefAsync(null!, WaitForState.Visible, TimeSpan.FromSeconds(1)));
    }

    /// <summary>Waits for the Wait For Ref Async Non Positive Timeout Throws Argument Out Of Range condition.</summary>
    [Fact]
    public async Task WaitForRefAsync_NonPositiveTimeout_ThrowsArgumentOutOfRange()
    {
        var session = CreateSession();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            session.WaitForRefAsync("s1e1", WaitForState.Visible, TimeSpan.Zero));
    }

    /// <summary>Waits for the Wait For Ref Async After Dispose Throws Object Disposed condition.</summary>
    [Fact]
    public async Task WaitForRefAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var session = CreateSession();
        session.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            session.WaitForRefAsync("s1e1", WaitForState.Visible, TimeSpan.FromMilliseconds(50)));
    }

    /// <summary>Waits for the Wait For Query Async No Condition Throws Argument Exception condition.</summary>
    [Fact]
    public async Task WaitForQueryAsync_NoCondition_ThrowsArgumentException()
    {
        var session = CreateSession();
        var query = new WaitForElementQuery(null, null, null, null);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            session.WaitForQueryAsync(query, WaitForState.Visible, TimeSpan.FromMilliseconds(50)));
    }

    /// <summary>Waits for the Wait For Query Async Detached State Throws Argument Exception condition.</summary>
    [Fact]
    public async Task WaitForQueryAsync_DetachedState_ThrowsArgumentException()
    {
        var session = CreateSession();
        var query = new WaitForElementQuery("OK", null, null, null);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            session.WaitForQueryAsync(query, WaitForState.Detached, TimeSpan.FromMilliseconds(50)));
    }

    /// <summary>Waits for the Wait For State Parser Parses Known Values condition.</summary>
    [Theory]
    [InlineData("attached", WaitForState.Attached)]
    [InlineData("detached", WaitForState.Detached)]
    [InlineData("visible", WaitForState.Visible)]
    [InlineData("hidden", WaitForState.Hidden)]
    [InlineData("enabled", WaitForState.Enabled)]
    [InlineData("disabled", WaitForState.Disabled)]
    [InlineData("VISIBLE", WaitForState.Visible)]
    [InlineData("Enabled", WaitForState.Enabled)]
    public void WaitForStateParser_ParsesKnownValues(string wire, WaitForState expected)
    {
        Assert.True(WaitForStateParser.TryParse(wire, out var actual));
        Assert.Equal(expected, actual);
    }

    /// <summary>Waits for the Wait For State Parser Rejects Unknown Values condition.</summary>
    [Theory]
    [InlineData("focused")]
    [InlineData("")]
    [InlineData(null)]
    public void WaitForStateParser_RejectsUnknownValues(string? wire)
    {
        Assert.False(WaitForStateParser.TryParse(wire, out _));
    }

    /// <summary>Waits for the Wait For State Parser To Wire String Returns Lowercase condition.</summary>
    [Fact]
    public void WaitForStateParser_ToWireString_ReturnsLowercase()
    {
        Assert.Equal("visible", WaitForStateParser.ToWireString(WaitForState.Visible));
        Assert.Equal("disabled", WaitForStateParser.ToWireString(WaitForState.Disabled));
    }

    /// <summary>Waits for the Wait For Element Query Has Any Condition condition.</summary>
    [Theory]
    [InlineData(null, null, null, null, false)]
    [InlineData("", "", "", "", false)]
    [InlineData("OK", null, null, null, true)]
    [InlineData(null, "Button", null, null, true)]
    [InlineData(null, null, "id", null, true)]
    [InlineData(null, null, null, "Edit", true)]
    public void WaitForElementQuery_HasAnyCondition(string? n, string? c, string? a, string? cn, bool expected)
    {
        var q = new WaitForElementQuery(n, c, a, cn);
        Assert.Equal(expected, q.HasAnyCondition);
    }

    /// <summary>Performs the Window Search Query Matches And Combined Regexes operation.</summary>
    [Fact]
    public void WindowSearchQuery_Matches_AndCombinedRegexes()
    {
        var info = new WindowInfo(
            ProcessId: 1,
            ProcessName: "Notepad",
            Title: "Untitled - Notepad",
            ControlType: "Window",
            ClassName: "Notepad",
            NativeWindowHandle: 0x10);

        Assert.True(new WindowSearchQuery("Untitled", null, null, null).Matches(info, executablePath: null));
        Assert.True(new WindowSearchQuery(null, "notepad", null, null).Matches(info, executablePath: null));
        Assert.True(new WindowSearchQuery(null, null, "^notepad$", null).Matches(info, executablePath: null));
        Assert.True(new WindowSearchQuery("Untitled", "Notepad", "notepad", null).Matches(info, executablePath: null));

        Assert.False(new WindowSearchQuery("calculator", null, null, null).Matches(info, executablePath: null));
        // Title match true but ProcessName regex fails => AND false
        Assert.False(new WindowSearchQuery("Untitled", null, "calculator", null).Matches(info, executablePath: null));
    }

    /// <summary>Performs the Window Search Query Matches Against Executable Path operation.</summary>
    [Fact]
    public void WindowSearchQuery_Matches_AgainstExecutablePath()
    {
        var info = new WindowInfo(1, "notepad", "x", "Window", null, 0);
        var q = new WindowSearchQuery(null, null, null, @"\\Windows\\System32\\notepad\.exe");
        Assert.True(q.Matches(info, executablePath: @"C:\Windows\System32\notepad.exe"));
        Assert.False(q.Matches(info, executablePath: null));
        Assert.False(q.Matches(info, executablePath: @"C:\Other\notepad.exe"));
    }

    /// <summary>Performs the Window Search Query Matches Bad Regex Returns False operation.</summary>
    [Fact]
    public void WindowSearchQuery_Matches_BadRegexReturnsFalse()
    {
        var info = new WindowInfo(1, "notepad", "x", "Window", null, 0);
        var q = new WindowSearchQuery("[unterminated", null, null, null);
        Assert.False(q.Matches(info, executablePath: null));
    }

    /// <summary>Waits for the Wait For Result Is Record With Value Equality condition.</summary>
    [Fact]
    public void WaitForResult_IsRecordWithValueEquality()
    {
        var a = new WaitForResult("s1e1", WaitForState.Visible);
        var b = new WaitForResult("s1e1", WaitForState.Visible);
        Assert.Equal(a, b);
    }

    /// <summary>Waits for the Wait For Ref Async Cancelled Throws Operation Canceled Exception condition.</summary>
    [Fact]
    public async Task WaitForRefAsync_Cancelled_ThrowsOperationCanceledException()
    {
        var button = FakeElement.Button("OK");
        var root = FakeElement.Window("Fake Window", button);
        var session = CreateSessionWithRoot(root);

        await session.SnapshotAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            session.WaitForRefAsync("s1e2", WaitForState.Visible, TimeSpan.FromSeconds(1), cts.Token));
    }

    /// <summary>Waits for the Wait For Query Async Cancelled Throws Operation Canceled Exception condition.</summary>
    [Fact]
    public async Task WaitForQueryAsync_Cancelled_ThrowsOperationCanceledException()
    {
        var root = FakeElement.Window("Fake Window", FakeElement.Button("OK"));
        var session = CreateSessionWithRoot(root);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            session.WaitForQueryAsync(
                new WaitForElementQuery("OK", null, null, null),
                WaitForState.Visible,
                TimeSpan.FromSeconds(1),
                cts.Token));
    }

    /// <summary>Waits for the Wait For Ref Async Visible Not Satisfied Throws Wait Timeout Exception condition.</summary>
    [Fact]
    public async Task WaitForRefAsync_VisibleNotSatisfied_ThrowsWaitTimeoutException()
    {
        var button = FakeElement.Button("OK");
        button.IsOffscreen = true;
        var root = FakeElement.Window("Fake Window", button);
        var session = CreateSessionWithRoot(root);

        await session.SnapshotAsync();
        var ex = await Assert.ThrowsAsync<WaitTimeoutException>(() =>
            session.WaitForRefAsync("s1e2", WaitForState.Visible, TimeSpan.FromMilliseconds(200)));

        Assert.Contains("wait-for did not observe state 'visible'", ex.Message, StringComparison.Ordinal);
        Assert.Contains("s1e2", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>Waits for the Wait For Query Async Visible Match Returns Matching Ref condition.</summary>
    [Fact]
    public async Task WaitForQueryAsync_VisibleMatch_ReturnsMatchingRef()
    {
        var root = FakeElement.Window("Fake Window", FakeElement.Button("OK", automationId: "okButton"));
        var session = CreateSessionWithRoot(root);

        var result = await session.WaitForQueryAsync(
            new WaitForElementQuery("OK", "Button", "okButton", null),
            WaitForState.Visible,
            TimeSpan.FromSeconds(1));

        Assert.Equal("s1e2", result.Ref);
        Assert.Equal(WaitForState.Visible, result.State);
    }

    /// <summary>Waits for the Wait For Query Async Hidden Not Satisfied Throws Wait Timeout Exception condition.</summary>
    [Fact]
    public async Task WaitForQueryAsync_HiddenNotSatisfied_ThrowsWaitTimeoutException()
    {
        var button = FakeElement.Button("OK");
        button.IsOffscreen = false;
        var root = FakeElement.Window("Fake Window", button);
        var session = CreateSessionWithRoot(root);

        var ex = await Assert.ThrowsAsync<WaitTimeoutException>(() =>
            session.WaitForQueryAsync(
                new WaitForElementQuery("OK", null, null, null),
                WaitForState.Hidden,
                TimeSpan.FromMilliseconds(200)));

        Assert.Contains("wait-for did not observe a matching element", ex.Message, StringComparison.Ordinal);
    }
}
