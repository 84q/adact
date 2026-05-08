using Adact.Engine;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Operation Blocker Detector behavior.</summary>
[Trait("Layer", "Unit")]
public class OperationBlockerDetectorTests
{
    private sealed class FakeApi : OperationBlockerDetector.IApi
    {
        /// <summary>Gets or sets the Session Locked value.</summary>
        public bool SessionLocked { get; set; }
        /// <summary>Gets or sets the Secure Desktop Active value.</summary>
        public bool? SecureDesktopActive { get; set; }
        /// <summary>Gets or sets the Window Visible value.</summary>
        public bool WindowVisible { get; set; } = true;
        /// <summary>Gets or sets the Foreground Window value.</summary>
        public nint ForegroundWindow { get; set; }
        /// <summary>Gets or sets the Foreground Window Locked Screen value.</summary>
        public bool ForegroundWindowLockedScreen { get; set; }

        /// <summary>Gets a value indicating whether Is Session Locked.</summary>
        public bool IsSessionLocked(int sessionId) => SessionLocked;
        /// <summary>Gets a value indicating whether Is Secure Desktop Active.</summary>
        public bool? IsSecureDesktopActive() => SecureDesktopActive;
        /// <summary>Gets a value indicating whether Is Window Visible.</summary>
        public bool IsWindowVisible(nint hwnd) => WindowVisible;
        /// <summary>Gets the Get Foreground Window value.</summary>
        public nint GetForegroundWindow() => ForegroundWindow;
        /// <summary>Gets a value indicating whether Is Foreground Window Locked Screen.</summary>
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

    /// <summary>Performs the Detect Session Locked Returns Blocked operation.</summary>
    [Fact]
    public void Detect_SessionLocked_ReturnsBlocked()
    {
        var fake = new FakeApi { SessionLocked = true };
        var result = DetectWithFake(fake);

        Assert.True(result.IsBlocked);
        Assert.Equal("desktop session is locked", result.Reason);
    }

    /// <summary>Performs the Detect Secure Desktop Active Returns Blocked operation.</summary>
    [Fact]
    public void Detect_SecureDesktopActive_ReturnsBlocked()
    {
        var fake = new FakeApi { SecureDesktopActive = true };
        var result = DetectWithFake(fake);

        Assert.True(result.IsBlocked);
        Assert.Equal("secure desktop is active (UAC prompt or login screen)", result.Reason);
    }

    /// <summary>Performs the Detect Foreground Window Locked Screen Returns Blocked operation.</summary>
    [Fact]
    public void Detect_ForegroundWindowLockedScreen_ReturnsBlocked()
    {
        var fake = new FakeApi { ForegroundWindowLockedScreen = true };
        var result = DetectWithFake(fake);

        Assert.True(result.IsBlocked);
        Assert.Equal("desktop session is locked (lock screen is in foreground)", result.Reason);
    }

    /// <summary>Performs the Detect Window Not Visible Returns Blocked operation.</summary>
    [Fact]
    public void Detect_WindowNotVisible_ReturnsBlocked()
    {
        var fake = new FakeApi { WindowVisible = false };
        var result = DetectWithFake(fake);

        Assert.True(result.IsBlocked);
        Assert.Equal("target window is not visible or not in the foreground", result.Reason);
    }

    /// <summary>Performs the Detect Window Not Foreground Returns Blocked operation.</summary>
    [Fact]
    public void Detect_WindowNotForeground_ReturnsBlocked()
    {
        var fake = new FakeApi { ForegroundWindow = 0x5678 };
        var result = DetectWithFake(fake, hwnd: 0x1234);

        Assert.True(result.IsBlocked);
        Assert.Equal("target window is not visible or not in the foreground", result.Reason);
    }

    /// <summary>Performs the Detect Normal State Returns Not Blocked operation.</summary>
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

    /// <summary>Performs the Detect Invalid Hwnd Skips Window Check And Returns Not Blocked operation.</summary>
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
        var result = DetectWithFake(fake, hwnd: 0);

        Assert.False(result.IsBlocked);
        Assert.Null(result.Reason);
    }

    /// <summary>Performs the Detect Api Throws Falls Back To Not Blocked operation.</summary>
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
        /// <summary>Gets a value indicating whether Is Session Locked.</summary>
        public bool IsSessionLocked(int sessionId) => throw new InvalidOperationException("fail");
        /// <summary>Gets a value indicating whether Is Secure Desktop Active.</summary>
        public bool? IsSecureDesktopActive() => throw new InvalidOperationException("fail");
        /// <summary>Gets a value indicating whether Is Window Visible.</summary>
        public bool IsWindowVisible(nint hwnd) => throw new InvalidOperationException("fail");
        /// <summary>Gets the Get Foreground Window value.</summary>
        public nint GetForegroundWindow() => throw new InvalidOperationException("fail");
        /// <summary>Gets a value indicating whether Is Foreground Window Locked Screen.</summary>
        public bool IsForegroundWindowLockedScreen() => throw new InvalidOperationException("fail");
    }
}
