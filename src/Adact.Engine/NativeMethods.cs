using System.Runtime.InteropServices;

namespace Adact.Engine;

/// <summary>WindowSession 内部で使う Win32 ヘルパー (モーダルダイアログ検出など)。</summary>
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

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // 対話セッション判定 (InteractiveSessionGuard) で使用する WindowStation 名取得用 API。
    // 設計: discussion/018_対話セッション判定.md §5.1。
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetProcessWindowStation();

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
}
