namespace Adact.Cli.Snapshots;

/// <summary>
/// </summary>
/// <param name="AutomationId">UIA AutomationId。</param>
/// <param name="IsEnabled">UIA IsEnabled。</param>
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

internal sealed record SnapshotMeta(
  string SessionId,
  string? ProcessName,
  int? ProcessId,
  string GeneratedAt);
