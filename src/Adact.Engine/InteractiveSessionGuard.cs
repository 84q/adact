using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Adact.Engine;

/// <summary>
/// Detects whether the process is running in an interactive session.
/// </summary>
public static class InteractiveSessionGuard
{
    /// <summary>
    /// Indicates the interactive window station name.
    /// </summary>
    public const string InteractiveWindowStationName = "WinSta0";

    /// <summary>
    /// Error code returned when no interactive session is available.
    /// </summary>
    public const string ErrorCode = "NO_INTERACTIVE_SESSION";

    /// <summary>
    /// The result of an interactive-session check.
    /// </summary>
    /// <param name="Ok">Whether the session is interactive.</param>
    /// <param name="Message">A diagnostic message when the check fails.</param>
    public readonly record struct CheckResult(bool Ok, string? Message);

    /// <summary>
    /// The result of probing the current process session.
    /// </summary>
    /// <param name="Ok">Whether the session is interactive.</param>
    /// <param name="SessionId">The current process session ID.</param>
    /// <param name="WindowStationName">The current window station name.</param>
    /// <param name="Message">A diagnostic message when the probe fails.</param>
    public readonly record struct ProbeResult(
        bool Ok,
        int SessionId,
        string? WindowStationName,
        string? Message);

    /// <summary>
    /// Checks whether the current session is interactive.
    /// </summary>
    public static CheckResult Check(int sessionId, string? windowStationName)
    {
        if (sessionId == 0
            || windowStationName is null
            || !string.Equals(windowStationName, InteractiveWindowStationName, StringComparison.OrdinalIgnoreCase))
        {
            var message = $"daemon is not in an interactive desktop session "
                + $"(SessionId={sessionId}, WindowStation={FormatStationForMessage(windowStationName)})";
            return new CheckResult(false, message);
        }

        return new CheckResult(true, null);
    }

    /// <summary>
    /// Probes the current process session and window station.
    /// </summary>
    public static ProbeResult Probe()
    {
        int sessionId;
        try
        {
            sessionId = Process.GetCurrentProcess().SessionId;
        }
        catch
        {
            sessionId = 0;
        }

        string? windowStationName;
        try
        {
            windowStationName = TryGetWindowStationName();
        }
        catch
        {
            windowStationName = null;
        }

        var result = Check(sessionId, windowStationName);
        return new ProbeResult(result.Ok, sessionId, windowStationName, result.Message);
    }

    private static string FormatStationForMessage(string? name)
        => name is null ? "<unknown>" : $"\"{name}\"";

    /// <summary>
    /// Gets the current window station name.
    /// </summary>
    private static string? TryGetWindowStationName()
    {
        var hStation = NativeMethods.GetProcessWindowStation();
        if (hStation == IntPtr.Zero)
        {
            return null;
        }

        NativeMethods.GetUserObjectInformation(hStation, NativeMethods.UOI_NAME, IntPtr.Zero, 0, out var needed);
        if (needed == 0)
        {
            return null;
        }

        var buffer = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!NativeMethods.GetUserObjectInformation(hStation, NativeMethods.UOI_NAME, buffer, needed, out _))
            {
                return null;
            }
            return Marshal.PtrToStringUni(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
