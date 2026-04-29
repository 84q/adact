namespace Adact.Engine;

/// <summary>ListWindowsAsync の戻り値。トップレベルウィンドウのサマリ。</summary>
/// <param name="ProcessId">所有プロセス ID。</param>
/// <param name="ProcessName">所有プロセス名 (拡張子なし、取得失敗時は <c>"?"</c>)。</param>
/// <param name="Title">ウィンドウタイトル。空文字列の場合あり。</param>
/// <param name="ControlType">UIA ControlType 名 (例: <c>"Window"</c>、取得失敗時は <c>"Unknown"</c>)。</param>
/// <param name="ClassName">Win32 ウィンドウクラス名。空文字列は <c>null</c> 化されている。</param>
/// <param name="NativeWindowHandle">Win32 HWND。FromHandle / 各種 Win32 API への入力に用いる。</param>
public sealed record WindowInfo(
    int ProcessId,
    string ProcessName,
    string Title,
    string ControlType,
    string? ClassName,
    nint NativeWindowHandle);
