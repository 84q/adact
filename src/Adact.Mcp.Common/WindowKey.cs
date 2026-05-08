using System.Diagnostics;

using Adact.Engine;

namespace Adact.Mcp.Common;

/// <summary>
/// Identifies a top-level window by HWND and process identity.
/// </summary>
/// <param name="Hwnd">Win32 HWND.</param>
/// <param name="ProcessId">Owning process ID.</param>
/// <param name="ProcessStartTime">Owning process start time.</param>
public readonly record struct WindowKey(nint Hwnd, int ProcessId, DateTime ProcessStartTime)
{
    /// <summary>
    /// Creates a key from window metadata.
    /// </summary>
    public static WindowKey From(WindowInfo info)
    {
        DateTime startTime;
        try
        {
            using var p = Process.GetProcessById(info.ProcessId);
            startTime = p.StartTime;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            startTime = DateTime.MinValue;
        }
        return new WindowKey(info.NativeWindowHandle, info.ProcessId, startTime);
    }
}
