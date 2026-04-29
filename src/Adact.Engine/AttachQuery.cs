namespace Adact.Engine;

/// <summary>
/// AttachAsync 用クエリ。<see cref="ProcessName"/> または <see cref="WindowTitle"/> または
/// <see cref="ClassName"/> または <see cref="ProcessId"/> のいずれかを指定する。
/// 厳密一致 (ignore case) で照合し、複数ヒット時は <see cref="Adact.Engine.Exceptions.AmbiguousAttachException"/>。
/// </summary>
/// <param name="ProcessName">プロセス名。拡張子なし (例: <c>"notepad"</c>)。大文字小文字無視で完全一致。</param>
/// <param name="WindowTitle">ウィンドウタイトル文字列。大文字小文字無視で完全一致。</param>
/// <param name="ClassName">Win32 ウィンドウクラス名。大文字小文字無視で完全一致。</param>
/// <param name="ProcessId">プロセス ID。指定したプロセスが所有するウィンドウのみ対象とする。</param>
public sealed record AttachQuery(
    string? ProcessName = null,
    string? WindowTitle = null,
    string? ClassName = null,
    int? ProcessId = null)
{
    /// <summary>プロセス名のみを指定した <see cref="AttachQuery"/> を生成する。</summary>
    /// <param name="processName">対象プロセス名 (拡張子なし)。</param>
    /// <returns>新しい <see cref="AttachQuery"/> インスタンス。</returns>
    public static AttachQuery ByProcess(string processName) => new(ProcessName: processName);

    /// <summary>ウィンドウタイトルのみを指定した <see cref="AttachQuery"/> を生成する。</summary>
    /// <param name="windowTitle">対象ウィンドウのタイトル文字列。</param>
    /// <returns>新しい <see cref="AttachQuery"/> インスタンス。</returns>
    public static AttachQuery ByTitle(string windowTitle) => new(WindowTitle: windowTitle);

    /// <summary>プロセス ID のみを指定した <see cref="AttachQuery"/> を生成する。</summary>
    /// <param name="pid">対象プロセス ID。</param>
    /// <returns>新しい <see cref="AttachQuery"/> インスタンス。</returns>
    public static AttachQuery ByPid(int pid) => new(ProcessId: pid);
}
