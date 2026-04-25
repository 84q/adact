namespace Adact.Engine;

/// <summary>ListWindowsAsync の戻り値。トップレベルウィンドウのサマリ。</summary>
public sealed record WindowInfo(
    int ProcessId,
    string ProcessName,
    string Title,
    string ControlType,
    string? ClassName,
    nint NativeWindowHandle);
