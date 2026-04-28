namespace Adact.Engine;

/// <summary>
/// SnapshotAsync の結果。Phase 7 以降は raw 全要素・全フィールド JSON のみを返し、
/// フィルタ選択 (operable/raw) は CLI 側で適用する。
/// </summary>
public sealed record SnapshotResult(
    string Json,
    string SessionId,
    string WindowTitle,
    string ProcessName,
    int ProcessId,
    DateTimeOffset GeneratedAt);
