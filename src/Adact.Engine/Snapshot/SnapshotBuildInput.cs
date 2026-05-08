using Adact.Engine.Elements;

namespace Adact.Engine.Snapshot;

/// <summary>
/// Input used to build a UI snapshot.
/// </summary>
/// <param name="RootWindow">The root window element.</param>
/// <param name="ModalSiblings">Modal sibling elements attached to the window.</param>
/// <param name="PopupSiblings">Popup sibling elements attached to the window.</param>
/// <param name="Options">Snapshot generation options.</param>
/// <param name="WindowTitle">The window title.</param>
/// <param name="ProcessName">The owning process name.</param>
/// <param name="ProcessId">The owning process ID.</param>
/// <param name="GeneratedAt">The snapshot creation time.</param>
public sealed record SnapshotBuildInput(
    IElement RootWindow,
    IReadOnlyList<IElement> ModalSiblings,
    IReadOnlyList<IElement> PopupSiblings,
    SnapshotOptions Options,
    string WindowTitle,
    string ProcessName,
    int ProcessId,
    DateTimeOffset GeneratedAt);

/// <summary>
/// Result of a snapshot build operation.
/// </summary>
/// <param name="Json">The generated snapshot JSON.</param>
/// <param name="SessionId">The session ID used for refs in the snapshot.</param>
public sealed record SnapshotBuildResult(
    string Json,
    string SessionId);
