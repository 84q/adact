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

    // ------------------------------------------------------------------
    // UWP / Microsoft Store アプリ起動 (IApplicationActivationManager)
    // 設計 024 §2 / §10。`shell:AppsFolder\<AUMID>` 形式の起動に使用する。
    // ------------------------------------------------------------------

    /// <summary>ApplicationActivationManager の CLSID。</summary>
    internal static readonly Guid CLSID_ApplicationActivationManager =
        new("45BA127D-10A8-46EA-8AB7-56EA9078943C");

    /// <summary><see cref="IApplicationActivationManager"/> の IID (ComImport の Guid と一致)。</summary>
    internal static readonly Guid IID_IApplicationActivationManager =
        new("2E941141-7F97-4756-BA1D-9DECDE894A3D");

    /// <summary><see cref="IApplicationActivationManager.ActivateApplication"/> に渡すフラグ。NOERRORUI のみ使用。</summary>
    internal const int AO_NOERRORUI = 0x00000002;

    /// <summary>
    /// IApplicationActivationManager の COM インタフェース宣言。AUMID 経由で UWP / Packaged アプリを起動する。
    /// </summary>
    [ComImport]
    [Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IApplicationActivationManager
    {
        /// <summary>AUMID と任意の引数からアプリを起動し、起動した PID を返す。</summary>
        /// <param name="appUserModelId">対象アプリの AUMID。</param>
        /// <param name="arguments">アプリに渡すコマンドライン引数 (空文字列可)。</param>
        /// <param name="options"><see cref="AO_NOERRORUI"/> 等の起動オプション。</param>
        /// <param name="processId">起動したプロセス ID。</param>
        /// <returns>HRESULT。失敗時は <see cref="System.Runtime.InteropServices.COMException"/> として throw される。</returns>
        [PreserveSig]
        int ActivateApplication(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string? arguments,
            int options,
            out uint processId);

        /// <summary>未使用 (vtable スロット保持用)。</summary>
        /// <param name="appUserModelId">AUMID。</param>
        /// <param name="itemArray">対象アイテム。</param>
        /// <param name="verb">verb。</param>
        /// <param name="options">フラグ。</param>
        /// <param name="processId">PID。</param>
        /// <returns>HRESULT。</returns>
        [PreserveSig]
        int ActivateForFile(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            IntPtr itemArray,
            [MarshalAs(UnmanagedType.LPWStr)] string? verb,
            int options,
            out uint processId);

        /// <summary>未使用 (vtable スロット保持用)。</summary>
        /// <param name="appUserModelId">AUMID。</param>
        /// <param name="itemArray">対象アイテム。</param>
        /// <param name="verb">verb。</param>
        /// <param name="options">フラグ。</param>
        /// <param name="processId">PID。</param>
        /// <returns>HRESULT。</returns>
        [PreserveSig]
        int ActivateForProtocol(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            IntPtr itemArray,
            [MarshalAs(UnmanagedType.LPWStr)] string? verb,
            int options,
            out uint processId);
    }
}
