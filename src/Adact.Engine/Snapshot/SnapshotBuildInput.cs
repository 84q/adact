using Adact.Engine.Elements;

namespace Adact.Engine.Snapshot;

/// <summary>
/// SnapshotBuilder.Build に渡す入力 (Engine 内部のメタ情報)。
/// Phase 7 でフィルタは CLI 側に移譲したため、フィールドからは除外している。
/// </summary>
/// <param name="RootWindow">snapshot の起点となるウィンドウ要素。</param>
/// <param name="ModalSiblings">同プロセス内で検出されたモーダルダイアログ要素 (root の追加子として挿入される)。</param>
/// <param name="PopupSiblings">同プロセス内で検出された Popup ウィンドウ要素 (root の追加子として挿入される)。</param>
/// <param name="Options">snapshot のオプション (再帰深度上限など)。</param>
/// <param name="WindowTitle">root ウィンドウのタイトル (snapshot メタデータ用)。</param>
/// <param name="ProcessName">プロセス名 (snapshot メタデータ用)。</param>
/// <param name="ProcessId">プロセス ID (snapshot メタデータ用)。</param>
/// <param name="GeneratedAt">snapshot 生成時刻 (UTC、メタデータ用)。</param>
public sealed record SnapshotBuildInput(
    IElement RootWindow,
    IReadOnlyList<IElement> ModalSiblings,
    IReadOnlyList<IElement> PopupSiblings,
    SnapshotOptions Options,
    string WindowTitle,
    string ProcessName,
    int ProcessId,
    DateTimeOffset GeneratedAt);

/// <summary>SnapshotBuilder.Build の戻り値。</summary>
/// <param name="Json">構築された snapshot JSON (1 行・無整形)。</param>
/// <param name="SessionId">snapshot を生成したセッションの ID 文字列 (例: <c>"s1"</c>)。</param>
public sealed record SnapshotBuildResult(
    string Json,
    string SessionId);
