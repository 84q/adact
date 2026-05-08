namespace Adact.Engine;

/// <summary>
/// Describes the result of a snapshot operation.
/// </summary>
public sealed record SnapshotResult(
    string Json,
    string SessionId,
    string WindowTitle,
    string ProcessName,
    int ProcessId,
    DateTimeOffset GeneratedAt);
