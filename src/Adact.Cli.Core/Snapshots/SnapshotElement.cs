namespace Adact.Cli.Snapshots;

/// <summary>
/// snapshot 1 要素の中間表現 (Phase 7)。
/// MCP server から受信した raw JSON を <see cref="SnapshotJsonParser"/> でこの形にデシリアライズし、
/// <see cref="SnapshotTreeFilter"/> でフィルタリング後 <see cref="SnapshotTextFormatter"/> で
/// 新形式テキストに整形する。
/// </summary>
/// <param name="Role">UIA ControlType 名 (例: <c>Window</c>, <c>Button</c>)。</param>
/// <param name="Name">UIA Name プロパティ。値がない場合は null。</param>
/// <param name="AutomationId">UIA AutomationId。</param>
/// <param name="Value">ValuePattern.Value などから取得された現在値。</param>
/// <param name="IsEnabled">UIA IsEnabled。</param>
/// <param name="IsOffscreen">UIA IsOffscreen。operable フィルタでは子孫ごと除外される。</param>
/// <param name="HasKeyboardFocus">キーボードフォーカスを保持しているか。</param>
/// <param name="IsModalDialog">Engine がモーダルダイアログとして識別した要素であるか。</param>
/// <param name="Ref">要素を一意に識別する Ref ID (例: <c>s1e7</c>)。</param>
/// <param name="Children">子要素。<see cref="SnapshotTreeFilter"/> 適用後は flatten/除外で件数が変わる。</param>
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
/// <param name="SessionId">セッション ID (例: <c>s1</c>)。</param>
/// <param name="ProcessName">対象 window のプロセス名。不明なら null。</param>
/// <param name="ProcessId">対象 window のプロセス ID。不明なら null。</param>
/// <param name="GeneratedAt">snapshot 生成時刻 (ISO 8601 文字列)。</param>
internal sealed record SnapshotMeta(
  string SessionId,
  string? ProcessName,
  int? ProcessId,
  string GeneratedAt);
