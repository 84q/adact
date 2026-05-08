namespace Adact.Engine;

/// <summary>
/// Options that control snapshot generation.
/// </summary>
/// <param name="MaxDepth">Maximum traversal depth for snapshot generation.</param>
public sealed record SnapshotOptions(int MaxDepth = 64);
