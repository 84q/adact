namespace Adact.Engine;

/// <summary>SnapshotAsync のオプション。Phase 7 でフィルタ選択は CLI 側へ移譲したため、
/// Engine 側のオプションは再帰深度ガードのみに簡素化した。</summary>
/// <param name="MaxDepth">UIA ツリーを再帰探索する最大深度。0 以下を指定した場合は内部の既定値 (64) が使われる。</param>
public sealed record SnapshotOptions(
    int MaxDepth = 64);
