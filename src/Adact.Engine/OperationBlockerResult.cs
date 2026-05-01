namespace Adact.Engine;

/// <summary>
/// 操作ブロック検知の結果。
/// </summary>
public readonly record struct OperationBlockerResult(bool IsBlocked, string? Reason);
