using System.Text.Json;

using Adact.Engine;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Window Session Inspect Screenshot behavior.</summary>
[Trait("Layer", "Unit")]
public class WindowSessionInspectScreenshotTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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

    /// <summary>Performs the Inspect Async Null Ref Throws Argument Null Exception operation.</summary>
    [Fact]
    public async Task InspectAsync_NullRef_ThrowsArgumentNullException()
    {
        var session = CreateSession();
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.InspectAsync(null!));
    }

    /// <summary>Performs the Inspect Async After Dispose Throws Object Disposed operation.</summary>
    [Fact]
    public async Task InspectAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var session = CreateSession();
        session.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.InspectAsync("s1e1"));
    }

    /// <summary>Performs the Screenshot Async After Dispose Throws Object Disposed operation.</summary>
    [Fact]
    public async Task ScreenshotAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var session = CreateSession();
        session.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ScreenshotAsync(null, null));
    }

    /// <summary>Performs the Inspect Result Is Record With Expected Properties operation.</summary>
    [Fact]
    public void InspectResult_IsRecordWithExpectedProperties()
    {
        var rect = new Rect(10, 20, 100, 50);
        var patterns = new Dictionary<string, IReadOnlyDictionary<string, object?>>
        {
            ["Toggle"] = new Dictionary<string, object?> { ["ToggleState"] = "On" },
        };
        var r = new InspectResult(
            Ref: "s1e7",
            Name: "OK",
            ControlType: "Button",
            AutomationId: "okBtn",
            ClassName: "Button",
            HelpText: null,
            Value: null,
            BoundingRect: rect,
            IsEnabled: true,
            IsOffscreen: false,
            IsKeyboardFocusable: true,
            HasKeyboardFocus: false,
            Patterns: patterns);

        Assert.Equal("s1e7", r.Ref);
        Assert.Equal("Button", r.ControlType);
        Assert.Equal(rect, r.BoundingRect);
        Assert.True(r.Patterns.ContainsKey("Toggle"));
        Assert.Equal("On", r.Patterns["Toggle"]["ToggleState"]);

        var r2 = r with { Name = "OK" };
        Assert.Equal(r, r2);
    }

    /// <summary>Performs the Screenshot Result Is Record With Expected Properties operation.</summary>
    [Fact]
    public void ScreenshotResult_IsRecordWithExpectedProperties()
    {
        var r = new ScreenshotResult(Path: "C:\\tmp\\a.png", Width: 640, Height: 480);
        Assert.Equal("C:\\tmp\\a.png", r.Path);
        Assert.Equal(640, r.Width);
        Assert.Equal(480, r.Height);

        var json = JsonSerializer.Serialize(r, CamelCaseOptions);
        Assert.Contains("\"path\":", json, StringComparison.Ordinal);
        Assert.Contains("\"width\":640", json, StringComparison.Ordinal);
        Assert.Contains("\"height\":480", json, StringComparison.Ordinal);
    }
}
