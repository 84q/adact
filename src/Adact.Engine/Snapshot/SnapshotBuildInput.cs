using Adact.Engine.Elements;
using Adact.Engine.Filters;

namespace Adact.Engine.Snapshot;

/// <summary>SnapshotBuilder.Build に渡す入力 (Engine 内部のメタ情報)。</summary>
public sealed record SnapshotBuildInput(
    IElement RootWindow,
    IReadOnlyList<IElement> ModalSiblings,
    IFilterStrategy Filter,
    SnapshotOptions Options,
    string WindowTitle,
    string ProcessName,
    int ProcessId,
    DateTimeOffset GeneratedAt);

/// <summary>SnapshotBuilder.Build の戻り値。</summary>
public sealed record SnapshotBuildResult(
    string Json,
    string SessionId);
