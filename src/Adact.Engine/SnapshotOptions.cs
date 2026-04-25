namespace Adact.Engine;

/// <summary>SnapshotAsync のオプション。</summary>
public sealed record SnapshotOptions(
    string FilterName = "operable",
    int MaxDepth = 64);
