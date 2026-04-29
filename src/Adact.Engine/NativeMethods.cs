using System.Runtime.InteropServices;

namespace Adact.Engine;

/// <summary>WindowSession 内部で使う Win32 ヘルパー (モーダルダイアログ検出など)。</summary>
internal static class NativeMethods
{
    /// <summary>トップレベルウィンドウを列挙する user32 EnumWindows を呼ぶ。</summary>
    /// <param name="lpEnumFunc">各ウィンドウについて呼ばれるコールバック。</param>
    /// <param name="lParam">コールバックに渡す任意データ。</param>
    /// <returns>列挙が成功した場合 true。</returns>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    /// <summary>user32 GetWindow の P/Invoke シグネチャ。Owner / Next / Prev 等の関連ウィンドウを取得する。</summary>
    /// <param name="hWnd">基準ウィンドウハンドル。</param>
    /// <param name="uCmd">取得するウィンドウの種別 (例: <see cref="GW_OWNER"/>)。</param>
    /// <returns>該当ウィンドウの HWND。なければ <see cref="IntPtr.Zero"/>。</returns>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    /// <summary>user32 IsWindowEnabled の P/Invoke シグネチャ。</summary>
    /// <param name="hWnd">判定対象の HWND。</param>
    /// <returns>ウィンドウが enable 状態であれば true。</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowEnabled(IntPtr hWnd);

    /// <summary>user32 IsWindowVisible の P/Invoke シグネチャ。</summary>
    /// <param name="hWnd">判定対象の HWND。</param>
    /// <returns>ウィンドウが可視であれば true。</returns>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(IntPtr hWnd);

    /// <summary>user32 GetWindowThreadProcessId の P/Invoke シグネチャ。</summary>
    /// <param name="hWnd">対象ウィンドウの HWND。</param>
    /// <param name="lpdwProcessId">取得されたプロセス ID。</param>
    /// <returns>作成スレッド ID。</returns>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>user32 PostMessage の P/Invoke シグネチャ (WM_CLOSE 送信用)。</summary>
    /// <param name="hWnd">宛先ウィンドウの HWND。</param>
    /// <param name="Msg">メッセージ ID (例: <see cref="WM_CLOSE"/>)。</param>
    /// <param name="wParam">wParam。</param>
    /// <param name="lParam">lParam。</param>
    /// <returns>送信に成功した場合 true。</returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // 対話セッション判定 (InteractiveSessionGuard) で使用する WindowStation 名取得用 API。
    // 設計: discussion/018_対話セッション判定.md §5.1。
    /// <summary>現プロセスの WindowStation ハンドルを返す user32 API。失敗時は <see cref="IntPtr.Zero"/>。</summary>
    /// <returns>WindowStation ハンドル。</returns>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetProcessWindowStation();

    /// <summary>
    /// user32 GetUserObjectInformationW の P/Invoke シグネチャ。WindowStation 名取得に使う。
    /// </summary>
    /// <param name="hObj">対象オブジェクトハンドル。</param>
    /// <param name="nIndex">取得情報の種別 (例: <see cref="UOI_NAME"/>)。</param>
    /// <param name="pvInfo">受け取りバッファ。</param>
    /// <param name="nLength"><paramref name="pvInfo"/> のサイズ (バイト)。</param>
    /// <param name="lpnLengthNeeded">実際に必要だったバイト数。</param>
    /// <returns>成功した場合 true。</returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetUserObjectInformationW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetUserObjectInformation(
        IntPtr hObj,
        int nIndex,
        IntPtr pvInfo,
        uint nLength,
        out uint lpnLengthNeeded);

    /// <summary>GetUserObjectInformation の nIndex 値。オブジェクト名を取得する。</summary>
    internal const int UOI_NAME = 2;

    /// <summary>GetWindow の uCmd 値。Owner ウィンドウを取得する。</summary>
    internal const uint GW_OWNER = 4;

    /// <summary>PostMessage に使うメッセージ ID。ウィンドウを閉じるよう要請する。</summary>
    internal const uint WM_CLOSE = 0x0010;

    /// <summary>EnumWindows のコールバックデリゲート。上記 <see cref="EnumWindows"/> 参照。</summary>
    /// <param name="hWnd">列挙中のウィンドウハンドル。</param>
    /// <param name="lParam">EnumWindows に渡した lParam。</param>
    /// <returns>列挙を継続する場合 true、中断する場合 false。</returns>
    internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
}
