using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Adact.Engine;

/// <summary>
/// </summary>
internal static class OperationBlockerDetector
{
    /// <summary>
    /// </summary>
    internal interface IApi
    {
        bool IsSessionLocked(int sessionId);
        bool? IsSecureDesktopActive();
        bool IsWindowVisible(nint hwnd);
        nint GetForegroundWindow();
        bool IsForegroundWindowLockedScreen();
    }

    private sealed class Win32Api : IApi
    {
        public bool IsSessionLocked(int sessionId)
        {
            if (!NativeMethods.WTSQuerySessionInformation(
                    NativeMethods.WTS_CURRENT_SERVER_HANDLE,
                    sessionId,
                    NativeMethods.WTS_SESSIONSTATE_LOCK,
                    out var buffer,
                    out _))
            {
                return false;
            }

            try
            {
                var state = Marshal.ReadInt32(buffer);
                // WTS_SESSIONSTATE_LOCK: 0 = unlock, 1 = lock
                return state != 0;
            }
            finally
            {
                NativeMethods.WTSFreeMemory(buffer);
            }
        }

        public bool? IsSecureDesktopActive()
        {
            var hDesk = NativeMethods.OpenInputDesktop(0, false, NativeMethods.DESKTOP_READOBJECTS);
            if (hDesk == IntPtr.Zero)
                return null;

            try
            {
                if (!NativeMethods.GetUserObjectInformation(hDesk, NativeMethods.UOI_NAME, IntPtr.Zero, 0, out var needed))
                {
                    var err = Marshal.GetLastWin32Error();
                    if (err != 122) // ERROR_INSUFFICIENT_BUFFER
                        return null;
                }

                var buf = Marshal.AllocHGlobal((int)needed);
                try
                {
                    if (!NativeMethods.GetUserObjectInformation(hDesk, NativeMethods.UOI_NAME, buf, needed, out _))
                        return null;

                    var name = Marshal.PtrToStringUni(buf);
                    return name is not null && !name.Equals("Default", StringComparison.OrdinalIgnoreCase);
                }
                finally
                {
                    Marshal.FreeHGlobal(buf);
                }
            }
            finally
            {
                NativeMethods.CloseDesktop(hDesk);
            }
        }

        public bool IsWindowVisible(nint hwnd)
            => NativeMethods.IsWindowVisible(hwnd);

        public nint GetForegroundWindow()
            => NativeMethods.GetForegroundWindow();

        public bool IsForegroundWindowLockedScreen()
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == 0)
                return false;

            try
            {
                _ = NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
                using var p = Process.GetProcessById((int)pid);
                var name = p.ProcessName;
                return name.Equals("LockApp", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("LogonUI", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }

    internal static IApi? TestApi;

    /// <summary>
    /// </summary>
    internal static OperationBlockerResult Detect(int sessionId, nint windowHwnd)
    {
        var api = TestApi ?? new Win32Api();

        try
        {
            if (api.IsSessionLocked(sessionId))
                return new OperationBlockerResult(true, "desktop session is locked");
        }
        catch
        {
        }

        try
        {
            if (api.IsSecureDesktopActive() is true)
                return new OperationBlockerResult(true, "secure desktop is active (UAC prompt or login screen)");
        }
        catch
        {
        }

        try
        {
            if (api.IsForegroundWindowLockedScreen())
                return new OperationBlockerResult(true, "desktop session is locked (lock screen is in foreground)");
        }
        catch
        {
        }

        try
        {
            if (windowHwnd != 0)
            {
                if (!api.IsWindowVisible(windowHwnd))
                    return new OperationBlockerResult(true, "target window is not visible or not in the foreground");

                var fg = api.GetForegroundWindow();
                if (fg != 0 && fg != windowHwnd)
                    return new OperationBlockerResult(true, "target window is not visible or not in the foreground");
            }
            else
            {
            }
        }
        catch
        {
        }

        return new OperationBlockerResult(false, null);
    }
}
