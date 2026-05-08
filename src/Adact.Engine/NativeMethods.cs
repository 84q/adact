using System.Runtime.InteropServices;

namespace Adact.Engine;

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowEnabled(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <param name="hWnd">Destination window handle.</param>
    /// <param name="Msg">Message identifier.</param>
    /// <param name="wParam">Additional message-specific data.</param>
    /// <param name="lParam">Additional message-specific data.</param>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetProcessWindowStation();

    /// <summary>
    /// Retrieves information about a window object.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetUserObjectInformationW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetUserObjectInformation(
        IntPtr hObj,
        int nIndex,
        IntPtr pvInfo,
        uint nLength,
        out uint lpnLengthNeeded);

    internal const int UOI_NAME = 2;

    internal const uint GW_OWNER = 4;

    internal const uint WM_CLOSE = 0x0010;

    internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // ------------------------------------------------------------------
    // ------------------------------------------------------------------

    internal static readonly IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

    internal const int WTS_SESSIONSTATE_LOCK = 25;

    [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "WTSQuerySessionInformationW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WTSQuerySessionInformation(
        IntPtr hServer,
        int sessionId,
        int wtsInfoClass,
        out IntPtr ppBuffer,
        out uint pBytesReturned);

    [DllImport("wtsapi32.dll")]
    internal static extern void WTSFreeMemory(IntPtr pMemory);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr OpenInputDesktop(uint dwFlags, [MarshalAs(UnmanagedType.Bool)] bool fInherit, uint dwDesiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseDesktop(IntPtr hDesktop);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    internal const uint DESKTOP_READOBJECTS = 0x0001;

    // ------------------------------------------------------------------
    // ------------------------------------------------------------------

    internal static readonly Guid CLSID_ApplicationActivationManager =
        new("45BA127D-10A8-46EA-8AB7-56EA9078943C");

    internal static readonly Guid IID_IApplicationActivationManager =
        new("2E941141-7F97-4756-BA1D-9DECDE894A3D");

    internal const int AO_NOERRORUI = 0x00000002;

    /// <summary>
    /// Exposes the Application Activation Manager COM interface.
    /// </summary>
    [ComImport]
    [Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IApplicationActivationManager
    {
        [PreserveSig]
        int ActivateApplication(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string? arguments,
            int options,
            out uint processId);

        /// <param name="appUserModelId">The application user model ID.</param>
        /// <param name="itemArray">Reserved shell item array pointer.</param>
        /// <param name="verb">The activation verb.</param>
        /// <param name="options">Activation options.</param>
        /// <param name="processId">The activated process ID.</param>
        /// <returns>HRESULT.</returns>
        [PreserveSig]
        int ActivateForFile(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            IntPtr itemArray,
            [MarshalAs(UnmanagedType.LPWStr)] string? verb,
            int options,
            out uint processId);

        /// <param name="appUserModelId">The application user model ID.</param>
        /// <param name="itemArray">Reserved shell item array pointer.</param>
        /// <param name="verb">The activation verb.</param>
        /// <param name="options">Activation options.</param>
        /// <param name="processId">The activated process ID.</param>
        /// <returns>HRESULT.</returns>
        [PreserveSig]
        int ActivateForProtocol(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            IntPtr itemArray,
            [MarshalAs(UnmanagedType.LPWStr)] string? verb,
            int options,
            out uint processId);
    }
}
