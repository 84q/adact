using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Adact.Engine;

/// <summary>
/// daemon プロセスが「対話デスクトップに属するセッションか」を起動時に判定するガード。
/// 設計: discussion/018_対話セッション判定.md §5。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Check(int, string?)"/> は観測値 (SessionId / WindowStation 名) を引数に取る純関数。
/// テスト容易性のため副作用を持たない。
/// </para>
/// <para>
/// <see cref="Probe()"/> は実プロセスから観測値を取得して <see cref="Check"/> を呼ぶ薄いラッパ。
/// P/Invoke 失敗時は NG 扱い (デスクトップに到達できない状況とみなす)。
/// </para>
/// </remarks>
public static class InteractiveSessionGuard
{
    /// <summary>WindowStation 比較に用いる対話デスクトップ名 (大文字小文字無視)。</summary>
    public const string InteractiveWindowStationName = "WinSta0";

    /// <summary>
    /// 判定 NG 時に CLI / MCP の双方で共通に用いるエラーコード。
    /// 設計: discussion/018_対話セッション判定.md §5.3。
    /// daemon 起動ガード固有のため CLI ErrorCodes / MCP ToolErrors には登録せず、本定数を canonical とする。
    /// </summary>
    public const string ErrorCode = "NO_INTERACTIVE_SESSION";

    /// <summary>判定結果。</summary>
    /// <param name="Ok">対話デスクトップに属していれば true。</param>
    /// <param name="Message">NG 時のエラーメッセージ (観測値を含む)。OK 時は null。</param>
    public readonly record struct CheckResult(bool Ok, string? Message);

    /// <summary>観測値付きの <see cref="Probe"/> 結果。</summary>
    /// <param name="Ok">対話デスクトップに属していれば true。</param>
    /// <param name="SessionId">観測した SessionId (取得失敗時は 0)。</param>
    /// <param name="WindowStationName">観測した WindowStation 名 (取得失敗時は null)。</param>
    /// <param name="Message">NG 時のメッセージ。OK 時は null。</param>
    public readonly record struct ProbeResult(
        bool Ok,
        int SessionId,
        string? WindowStationName,
        string? Message);

    /// <summary>
    /// 純関数判定。設計 §5.1 のルール:
    /// <list type="bullet">
    /// <item><description><paramref name="sessionId"/> が 0 → NG</description></item>
    /// <item><description><paramref name="windowStationName"/> が null または "WinSta0" 以外 (大文字小文字無視) → NG</description></item>
    /// <item><description>上記いずれにも該当しない → OK</description></item>
    /// </list>
    /// </summary>
    /// <param name="sessionId">対象プロセスの Windows セッション ID。</param>
    /// <param name="windowStationName">対象プロセスの WindowStation 名。取得失敗時は null。</param>
    /// <returns>判定結果。OK の場合 <see cref="CheckResult.Message"/> は null。</returns>
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
    /// 実プロセスから SessionId と WindowStation 名を観測し <see cref="Check"/> を呼ぶ。
    /// P/Invoke エラー等で観測値が得られない場合は NG 扱い。
    /// </summary>
    /// <returns>観測値とともに格納された <see cref="ProbeResult"/>。</returns>
    public static ProbeResult Probe()
    {
        int sessionId;
        try
        {
            sessionId = Process.GetCurrentProcess().SessionId;
        }
        catch
        {
            // 取得失敗 → 観測値 0 として Check に流し、NG として扱わせる。
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

    /// <summary>エラーメッセージ用に WindowStation 名を整形する。null は <c>&lt;unknown&gt;</c>、それ以外はダブルクォートで囲む。</summary>
    /// <param name="name">WindowStation 名。</param>
    /// <returns>整形済み文字列。</returns>
    private static string FormatStationForMessage(string? name)
        => name is null ? "<unknown>" : $"\"{name}\"";

    /// <summary>
    /// 現在のプロセスに関連付けられた WindowStation 名を P/Invoke (<c>GetProcessWindowStation</c> + <c>GetUserObjectInformation</c>)
    /// で取得する。取得不能のときは null。
    /// </summary>
    /// <returns>WindowStation 名。取得失敗時は null。</returns>
    private static string? TryGetWindowStationName()
    {
        var hStation = NativeMethods.GetProcessWindowStation();
        if (hStation == IntPtr.Zero)
        {
            return null;
        }

        // 1 回目: 必要バッファサイズを取得 (関数自体は失敗するが lpnLengthNeeded に必要バイト数が入る)。
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
            // GetUserObjectInformationW は終端 NUL 込みの UTF-16 文字列を書き込む。
            return Marshal.PtrToStringUni(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
