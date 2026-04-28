using Adact.Engine.Elements;

namespace Adact.Engine.Snapshot;

/// <summary>
/// SnapshotBuilder.Build に渡す入力 (Engine 内部のメタ情報)。
/// Phase 7 でフィルタは CLI 側に移譲したため、フィールドからは除外している。
/// </summary>
public sealed record SnapshotBuildInput(
    IElement RootWindow,
    IReadOnlyList<IElement> ModalSiblings,
    SnapshotOptions Options,
    string WindowTitle,
    string ProcessName,
    int ProcessId,
    DateTimeOffset GeneratedAt);

/// <summary>SnapshotBuilder.Build の戻り値。</summary>
public sealed record SnapshotBuildResult(
    string Json,
    string SessionId);
