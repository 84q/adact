namespace Adact.Engine;

/// <summary>
/// SnapshotAsync の結果。Phase 7 以降は raw 全要素・全フィールド JSON のみを返し、
/// フィルタ選択 (operable/raw) は CLI 側で適用する。
/// </summary>
/// <param name="Json">UIA ツリーを表す JSON 文字列 (1 行・無整形)。<see cref="Snapshot.SnapshotBuilder"/> が生成する。</param>
/// <param name="SessionId">snapshot を生成したセッションの ID 文字列 (例: <c>"s1"</c>)。Ref ID のセッション部と一致する。</param>
/// <param name="WindowTitle">snapshot 採取時点の対象ウィンドウのタイトル。</param>
/// <param name="ProcessName">対象プロセス名 (拡張子なし)。</param>
/// <param name="ProcessId">対象プロセス ID。</param>
/// <param name="GeneratedAt">snapshot 生成時刻 (UTC)。</param>
public sealed record SnapshotResult(
    string Json,
    string SessionId,
    string WindowTitle,
    string ProcessName,
    int ProcessId,
    DateTimeOffset GeneratedAt);
