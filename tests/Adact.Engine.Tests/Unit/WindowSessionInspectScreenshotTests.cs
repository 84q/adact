using System.Text.Json;

using Adact.Engine;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// <see cref="WindowSession.InspectAsync(string, CancellationToken)"/> および
/// <see cref="WindowSession.ScreenshotAsync(string?, string?, CancellationToken)"/>
/// (Phase 8 Step 6) の引数検証 / Dispose 後挙動 / 結果型シリアライズを検証する Unit テスト。
/// 実 UIA / FlaUI には依存せず、<see cref="WindowSession.CreateForTest(int, WindowInfo)"/> で生成した
/// 最小セッションに対して、UIA に到達する前に弾かれる例外のみを確認する。
/// </summary>
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

    /// <summary>InspectAsync は <c>refId</c> が null の場合 ArgumentNullException を投げる。</summary>
    [Fact]
    public async Task InspectAsync_NullRef_ThrowsArgumentNullException()
    {
        var session = CreateSession();
        await Assert.ThrowsAsync<ArgumentNullException>(() => session.InspectAsync(null!));
    }

    /// <summary>InspectAsync は Dispose 済みセッションで ObjectDisposedException を投げる。</summary>
    [Fact]
    public async Task InspectAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var session = CreateSession();
        session.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.InspectAsync("s1e1"));
    }

    /// <summary>ScreenshotAsync は Dispose 済みセッションで ObjectDisposedException を投げる。</summary>
    [Fact]
    public async Task ScreenshotAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var session = CreateSession();
        session.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.ScreenshotAsync(null, null));
    }

    /// <summary>InspectResult が record として全プロパティを保持し値等価性を持つことを確認する。</summary>
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

        // record の値等価性確認 (回帰防止)。
        var r2 = r with { Name = "OK" };
        Assert.Equal(r, r2);
    }

    /// <summary>ScreenshotResult が record として全プロパティを保持し、JSON 化で設計どおりの key 名 (path/width/height) を出すことを確認する。</summary>
    [Fact]
    public void ScreenshotResult_IsRecordWithExpectedProperties()
    {
        var r = new ScreenshotResult(Path: "C:\\tmp\\a.png", Width: 640, Height: 480);
        Assert.Equal("C:\\tmp\\a.png", r.Path);
        Assert.Equal(640, r.Width);
        Assert.Equal(480, r.Height);

        // JSON シリアライズで設計どおりの key 名 (path/width/height) を出すことを確認。
        var json = JsonSerializer.Serialize(r, CamelCaseOptions);
        Assert.Contains("\"path\":", json, StringComparison.Ordinal);
        Assert.Contains("\"width\":640", json, StringComparison.Ordinal);
        Assert.Contains("\"height\":480", json, StringComparison.Ordinal);
    }
}
