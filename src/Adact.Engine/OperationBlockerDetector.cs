using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Adact.Engine;

/// <summary>
/// UIA 操作失敗時に、デスクトップ状態が操作をブロックしているかをベストエフォートで診断する。
/// 設計: discussion/027_操作ブロック検知.md。
/// </summary>
internal static class OperationBlockerDetector
{
    /// <summary>
    /// Win32 API 呼び出しを抽象化する内部境界。Unit テストで差し替える。
    /// </summary>
    internal interface IApi
    {
        bool IsSessionLocked(int sessionId);
        bool? IsSecureDesktopActive();
        bool IsWindowVisible(nint hwnd);
        nint GetForegroundWindow();
        bool IsForegroundWindowLockedScreen();
    }

    /// <summary>実際の Win32 API を呼ぶ既定実装。</summary>
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
                // GetUserObjectInformationW で UOI_NAME を取得
                // 2回呼び出し: 1回目はサイズ取得、2回目はデータ取得
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

    /// <summary>テスト用に差し替え可能な API 実装。null の場合は Win32 API を使う。</summary>
    internal static IApi? TestApi;

    /// <summary>
    /// 指定されたセッション・ウィンドウ状態を診断し、操作がブロックされているかを返す。
    /// </summary>
    /// <param name="sessionId">現在の Windows セッション ID。</param>
    /// <param name="windowHwnd">操作対象ウィンドウの HWND。</param>
    /// <returns>ブロック判定結果。</returns>
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
            // フォールバック: 診断不能として次の判定へ
        }

        try
        {
            if (api.IsSecureDesktopActive() is true)
                return new OperationBlockerResult(true, "secure desktop is active (UAC prompt or login screen)");
        }
        catch
        {
            // フォールバック
        }

        try
        {
            if (api.IsForegroundWindowLockedScreen())
                return new OperationBlockerResult(true, "desktop session is locked (lock screen is in foreground)");
        }
        catch
        {
            // フォールバック
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
                // HWND が無効な場合はウィンドウ状態を診断不能とする (フォールバック)
            }
        }
        catch
        {
            // フォールバック
        }

        return new OperationBlockerResult(false, null);
    }
}
