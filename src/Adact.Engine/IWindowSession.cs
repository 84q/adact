using Adact.Engine.Snapshot;

namespace Adact.Engine;

/// <summary>
/// MCP 層が利用する window session 操作の境界。
/// Production では <see cref="WindowSession"/> が実装し、テストでは fake session を登録できるようにする。
/// </summary>
public interface IWindowSession : IDisposable
{
    /// <summary>Session ID の数値部分。</summary>
    int SessionId { get; }

    /// <summary>attach 対象プロセス名。</summary>
    string ProcessName { get; }

    /// <summary>attach 対象プロセス ID。</summary>
    int ProcessId { get; }

    /// <summary>attach 対象ウィンドウタイトル。</summary>
    string Title { get; }

    /// <summary>attach 対象ウィンドウの HWND。</summary>
    nint NativeWindowHandle { get; }

    /// <summary>UIA snapshot を取得する。</summary>
    Task<SnapshotResult> SnapshotAsync(SnapshotOptions? options = null, CancellationToken ct = default);

    /// <summary>要素をクリックする。</summary>
    Task ClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default);

    /// <summary>要素を詳細オプション付きでクリックする。</summary>
    Task ClickWithOptionsAsync(string refId, ClickOptions options, CancellationToken ct = default);

    /// <summary>要素をダブルクリックする。</summary>
    Task DoubleClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default);

    /// <summary>入力要素の値を設定する。</summary>
    Task FillAsync(string refId, string text, CancellationToken ct = default);

    /// <summary>要素または window へ key combo を送る。</summary>
    Task PressAsync(string key, string? refId = null, CancellationToken ct = default);

    /// <summary>単一 key を押下状態にする。</summary>
    Task KeyDownAsync(string key, CancellationToken ct = default);

    /// <summary>単一 key を解放する。</summary>
    Task KeyUpAsync(string key, CancellationToken ct = default);

    /// <summary>要素へ逐次入力する。</summary>
    Task TypeAsync(string? refId, string text, int delayMs = 0, CancellationToken ct = default);

    /// <summary>要素上へマウスカーソルを移動する。</summary>
    Task HoverAsync(string refId, IReadOnlyList<string>? modifiers = null, int? positionX = null, int? positionY = null, CancellationToken ct = default);

    /// <summary>マウスカーソルを指定 target へ移動する。</summary>
    Task MouseMoveAsync(MouseTarget target, CancellationToken ct = default);

    /// <summary>指定 target でマウスボタンを押下する。</summary>
    Task MouseDownAsync(MouseTarget target, MouseButton button, CancellationToken ct = default);

    /// <summary>指定 target でマウスボタンを解放する。</summary>
    Task MouseUpAsync(MouseTarget target, MouseButton button, CancellationToken ct = default);

    /// <summary>指定 target でマウスホイールを操作する。</summary>
    Task MouseWheelAsync(MouseTarget target, int deltaX, int deltaY, CancellationToken ct = default);

    /// <summary>toggle 要素を On にする。</summary>
    Task CheckAsync(string refId, CancellationToken ct = default);

    /// <summary>toggle 要素を Off にする。</summary>
    Task UncheckAsync(string refId, CancellationToken ct = default);

    /// <summary>list / combobox の item を選択する。</summary>
    Task SelectAsync(string refId, SelectionTarget[] targets, SelectionMode mode = SelectionMode.Replace, CancellationToken ct = default);

    /// <summary>要素へ keyboard focus を移す。</summary>
    Task FocusAsync(string refId, CancellationToken ct = default);

    /// <summary>要素を表示領域へ scroll する。</summary>
    Task ScrollIntoViewAsync(string refId, CancellationToken ct = default);

    /// <summary>ScrollPattern でコンテナをスクロールする。</summary>
    Task ScrollAsync(string refId, ScrollMode mode, CancellationToken ct = default);

    /// <summary>要素の詳細情報を取得する。</summary>
    Task<InspectResult> InspectAsync(string refId, CancellationToken ct = default);

    /// <summary>window または要素の screenshot を取得する。</summary>
    Task<ScreenshotResult> ScreenshotAsync(string? refId, string? outPath, CancellationToken ct = default);

    /// <summary>window size を変更する。片方 null 時は現在値を維持する。</summary>
    Task ResizeAsync(int? width, int? height, CancellationToken ct = default);

    /// <summary>window を最小化する。</summary>
    Task MinimizeAsync(CancellationToken ct = default);

    /// <summary>window を最大化する。</summary>
    Task MaximizeAsync(CancellationToken ct = default);

    /// <summary>window を通常表示へ戻す。</summary>
    Task RestoreAsync(CancellationToken ct = default);

    /// <summary>ref が指定 state になるまで待機する。</summary>
    Task<WaitForResult> WaitForRefAsync(string refId, WaitForState state, TimeSpan timeout, CancellationToken ct = default);

    /// <summary>query に一致する要素が指定 state になるまで待機する。</summary>
    Task<WaitForResult> WaitForQueryAsync(WaitForElementQuery query, WaitForState state, TimeSpan timeout, CancellationToken ct = default);

    /// <summary>window を close する。</summary>
    Task CloseAsync(CancellationToken ct = default);

    /// <summary>window の backing process を kill する。</summary>
    Task<KillMethod> KillAsync(bool force = false, int timeoutMs = 5000, CancellationToken ct = default);
}
