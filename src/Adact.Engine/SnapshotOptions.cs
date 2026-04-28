namespace Adact.Engine;

/// <summary>SnapshotAsync のオプション。Phase 7 でフィルタ選択は CLI 側へ移譲したため、
/// Engine 側のオプションは再帰深度ガードのみに簡素化した。</summary>
public sealed record SnapshotOptions(
    int MaxDepth = 64);
