namespace Adact.Engine;

/// <summary>SnapshotAsync の結果。JSON 文字列とメタ情報、tree を保持する。</summary>
public sealed record SnapshotResult(
    string Json,
    string SessionId,
    string FilterName,
    string WindowTitle,
    string ProcessName,
    int ProcessId,
    DateTimeOffset GeneratedAt);
