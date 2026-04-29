using System.Diagnostics;

using Adact.Engine;

namespace Adact.Mcp.Common;

/// <summary>
/// HWND が再利用される可能性に備え、(HWND, processId, processStartTime) の 3 点組で
/// top-level window を一意化するキー。WindowRefStore のエントリ識別子として使用する。
/// 詳細は discussion/009_Phase5設計.md §7.3 参照。
/// </summary>
/// <param name="Hwnd">Win32 HWND。</param>
/// <param name="ProcessId">window を所有するプロセスの PID。</param>
/// <param name="ProcessStartTime">プロセスの起動時刻。取得できない場合は <see cref="DateTime.MinValue"/>。</param>
public readonly record struct WindowKey(nint Hwnd, int ProcessId, DateTime ProcessStartTime)
{
    /// <summary>
    /// <see cref="WindowInfo"/> から WindowKey を構築する。
    /// プロセスへのアクセス権がない等で StartTime を取得できない場合は
    /// <see cref="DateTime.MinValue"/> でフォールバックする。
    /// </summary>
    /// <param name="info">もとになる <see cref="WindowInfo"/>。</param>
    /// <returns>同一性判定に使用する <see cref="WindowKey"/>。</returns>
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
