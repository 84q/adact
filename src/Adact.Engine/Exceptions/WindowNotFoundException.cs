namespace Adact.Engine.Exceptions;

/// <summary>
/// <see cref="UiaEngine.AttachByHandleAsync(nint, CancellationToken)"/> で指定 HWND に対応する
/// top-level window が現在見つからなかった、または UIA から再取得できなかった場合に投げられる例外。
/// </summary>
public sealed class WindowNotFoundException : AdactException
{
    /// <summary>失敗対象の HWND。</summary>
    public nint Hwnd { get; }

    /// <summary>新しいインスタンスを初期化する。</summary>
    /// <param name="hwnd">失敗対象の Win32 ウィンドウハンドル。</param>
    public WindowNotFoundException(nint hwnd)
        : base($"No window found for hwnd 0x{hwnd.ToInt64():X}.")
    {
        Hwnd = hwnd;
    }
}
