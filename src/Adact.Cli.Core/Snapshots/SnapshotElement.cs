namespace Adact.Cli.Snapshots;

/// <summary>
/// Represents a formatted snapshot element used by CLI output.
/// </summary>
/// <param name="Role">The UIA role or control type.</param>
/// <param name="Name">The element name.</param>
/// <param name="AutomationId">UIA AutomationId。</param>
/// <param name="Value">The element value text.</param>
/// <param name="IsEnabled">UIA IsEnabled。</param>
/// <param name="IsSelected">Whether the element is selected.</param>
/// <param name="IsOffscreen">Whether the element is offscreen.</param>
/// <param name="HasKeyboardFocus">Whether the element currently has keyboard focus.</param>
/// <param name="IsModalDialog">Whether the element represents a modal dialog.</param>
/// <param name="Ref">The stable element ref.</param>
/// <param name="Children">The child snapshot elements.</param>
internal sealed record SnapshotElement(
  string Role,
  string? Name,
  string? AutomationId,
  string? Value,
  bool IsEnabled,
  bool IsSelected,
  bool IsOffscreen,
  bool HasKeyboardFocus,
  bool IsModalDialog,
  string Ref,
  IReadOnlyList<SnapshotElement> Children);

/// <summary>
/// Represents snapshot-level metadata emitted by the server.
/// </summary>
/// <param name="SessionId">The session id that produced the snapshot.</param>
/// <param name="ProcessName">The optional target process name.</param>
/// <param name="ProcessId">The optional target process id.</param>
/// <param name="GeneratedAt">The timestamp when the snapshot was generated.</param>
internal sealed record SnapshotMeta(
  string SessionId,
  string? ProcessName,
  int? ProcessId,
  string GeneratedAt);
