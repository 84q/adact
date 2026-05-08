namespace Adact.Engine;

/// <summary>
/// Result of operation-blocker detection.
/// </summary>
public readonly record struct OperationBlockerResult(bool IsBlocked, string? Reason);
