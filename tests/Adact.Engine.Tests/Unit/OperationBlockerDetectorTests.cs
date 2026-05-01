using Adact.Engine;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// <see cref="OperationBlockerDetector.Detect"/> の純関数部分を mocking した L1 Unit テスト。
/// </summary>
[Trait("Layer", "Unit")]
public class OperationBlockerDetectorTests
{
    private sealed class FakeApi : OperationBlockerDetector.IApi
    {
        public bool SessionLocked { get; set; }
        public bool? SecureDesktopActive { get; set; }
        public bool WindowVisible { get; set; } = true;
        public nint ForegroundWindow { get; set; }
        public bool ForegroundWindowLockedScreen { get; set; }

        public bool IsSessionLocked(int sessionId) => SessionLocked;
        public bool? IsSecureDesktopActive() => SecureDesktopActive;
        public bool IsWindowVisible(nint hwnd) => WindowVisible;
        public nint GetForegroundWindow() => ForegroundWindow;
        public bool IsForegroundWindowLockedScreen() => ForegroundWindowLockedScreen;
    }

    private static OperationBlockerResult DetectWithFake(OperationBlockerDetector.IApi fake, int sessionId = 1, nint hwnd = 0x1234)
    {
        OperationBlockerDetector.TestApi = fake;
        try
        {
            return OperationBlockerDetector.Detect(sessionId, hwnd);
        }
        finally
        {
            OperationBlockerDetector.TestApi = null;
        }
    }

    [Fact]
    public void Detect_SessionLocked_ReturnsBlocked()
    {
        var fake = new FakeApi { SessionLocked = true };
        var result = DetectWithFake(fake);

        Assert.True(result.IsBlocked);
        Assert.Equal("desktop session is locked", result.Reason);
    }

    [Fact]
    public void Detect_SecureDesktopActive_ReturnsBlocked()
    {
        var fake = new FakeApi { SecureDesktopActive = true };
        var result = DetectWithFake(fake);

        Assert.True(result.IsBlocked);
        Assert.Equal("secure desktop is active (UAC prompt or login screen)", result.Reason);
    }

    [Fact]
    public void Detect_ForegroundWindowLockedScreen_ReturnsBlocked()
    {
        var fake = new FakeApi { ForegroundWindowLockedScreen = true };
        var result = DetectWithFake(fake);

        Assert.True(result.IsBlocked);
        Assert.Equal("desktop session is locked (lock screen is in foreground)", result.Reason);
    }

    [Fact]
    public void Detect_WindowNotVisible_ReturnsBlocked()
    {
        var fake = new FakeApi { WindowVisible = false };
        var result = DetectWithFake(fake);

        Assert.True(result.IsBlocked);
        Assert.Equal("target window is not visible or not in the foreground", result.Reason);
    }

    [Fact]
    public void Detect_WindowNotForeground_ReturnsBlocked()
    {
        var fake = new FakeApi { ForegroundWindow = 0x5678 };
        var result = DetectWithFake(fake, hwnd: 0x1234);

        Assert.True(result.IsBlocked);
        Assert.Equal("target window is not visible or not in the foreground", result.Reason);
    }

    [Fact]
    public void Detect_NormalState_ReturnsNotBlocked()
    {
        var fake = new FakeApi
        {
            SessionLocked = false,
            SecureDesktopActive = false,
            WindowVisible = true,
            ForegroundWindow = 0x1234,
        };
        var result = DetectWithFake(fake, hwnd: 0x1234);

        Assert.False(result.IsBlocked);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Detect_InvalidHwnd_SkipsWindowCheckAndReturnsNotBlocked()
    {
        var fake = new FakeApi
        {
            SessionLocked = false,
            SecureDesktopActive = false,
            WindowVisible = true,
            ForegroundWindow = 0,
        };
        // hwnd = 0 (IntPtr.Zero) は無効なのでウィンドウ状態診断をスキップする
        var result = DetectWithFake(fake, hwnd: 0);

        Assert.False(result.IsBlocked);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Detect_ApiThrows_FallsBackToNotBlocked()
    {
        var fake = new ThrowingApi();
        var result = DetectWithFake(fake);

        Assert.False(result.IsBlocked);
        Assert.Null(result.Reason);
    }

    private sealed class ThrowingApi : OperationBlockerDetector.IApi
    {
        public bool IsSessionLocked(int sessionId) => throw new InvalidOperationException("fail");
        public bool? IsSecureDesktopActive() => throw new InvalidOperationException("fail");
        public bool IsWindowVisible(nint hwnd) => throw new InvalidOperationException("fail");
        public nint GetForegroundWindow() => throw new InvalidOperationException("fail");
        public bool IsForegroundWindowLockedScreen() => throw new InvalidOperationException("fail");
    }
}
