namespace Adact.Cli.Snapshots;

/// <summary>
/// snapshot 1 要素の中間表現 (Phase 7)。
/// MCP server から受信した raw JSON を <see cref="SnapshotJsonParser"/> でこの形にデシリアライズし、
/// <see cref="SnapshotTreeFilter"/> でフィルタリング後 <see cref="SnapshotTextFormatter"/> で
/// 新形式テキストに整形する。
/// </summary>
internal sealed record SnapshotElement(
  string Role,
  string? Name,
  string? AutomationId,
  string? Value,
  bool IsEnabled,
  bool IsOffscreen,
  bool HasKeyboardFocus,
  bool IsModalDialog,
  string Ref,
  IReadOnlyList<SnapshotElement> Children);

/// <summary>snapshot のメタ情報。frontmatter 出力用。</summary>
internal sealed record SnapshotMeta(
  string SessionId,
  string? ProcessName,
  int? ProcessId,
  string GeneratedAt);
