namespace Adact.Engine;

/// <summary>Window information returned by <c>ListWindowsAsync</c>.</summary>
/// <param name="ProcessId">Process ID.</param>
/// <param name="ProcessName">Process name (may be <c>"?"</c> when unknown).</param>
/// <param name="Title">Window title.</param>
/// <param name="ControlType">UIA control type (for example, <c>"Window"</c> or <c>"Unknown"</c>).</param>
/// <param name="ClassName">Win32 class name. Can be <c>null</c>.</param>
/// <param name="NativeWindowHandle">Win32 HWND, used with <c>FromHandle</c> and other Win32 APIs.</param>
/// <param name="ProcessStartTimeUtc">Process start time in UTC. Can be <c>null</c>.</param>
public sealed record WindowInfo(
    int ProcessId,
    string ProcessName,
    string Title,
    string ControlType,
    string? ClassName,
    nint NativeWindowHandle,
    DateTimeOffset? ProcessStartTimeUtc = null);
