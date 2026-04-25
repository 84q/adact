namespace Adact.Engine;

/// <summary>
/// AttachAsync 用クエリ。<see cref="ProcessName"/> または <see cref="WindowTitle"/> または <see cref="ProcessId"/> のいずれかを指定する。
/// 厳密一致 (ignore case) で照合し、複数ヒット時は <see cref="Adact.Engine.Exceptions.AmbiguousAttachException"/>。
/// </summary>
public sealed record AttachQuery(
    string? ProcessName = null,
    string? WindowTitle = null,
    int? ProcessId = null)
{
    public static AttachQuery ByProcess(string processName) => new(ProcessName: processName);
    public static AttachQuery ByTitle(string windowTitle) => new(WindowTitle: windowTitle);
    public static AttachQuery ByPid(int pid) => new(ProcessId: pid);
}
