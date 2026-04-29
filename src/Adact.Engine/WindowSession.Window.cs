using Adact.Engine.Exceptions;

using FlaUI.Core.Definitions;

namespace Adact.Engine;

public sealed partial class WindowSession
{
    /// <summary>
    /// アタッチ済みウィンドウのサイズを変更する。UIA <c>TransformPattern.Resize</c> を使う。
    /// Pattern 不在 / <c>CanResize</c> が false の場合は <see cref="ElementInteractionException"/>。
    /// </summary>
    /// <param name="width">新しい幅 (px)。0 より大きい値。</param>
    /// <param name="height">新しい高さ (px)。0 より大きい値。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException">width / height が 0 以下。</exception>
    /// <exception cref="ElementInteractionException">TransformPattern 不在 / 操作失敗の場合。</exception>
    public Task ResizeAsync(int width, int height, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "width must be > 0.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "height must be > 0.");
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                var transform = _window.Patterns.Transform.PatternOrDefault;
                if (transform is null || !transform.CanResize.ValueOrDefault)
                {
                    throw new ElementInteractionException(string.Empty, "resize",
                        "Window does not support resize (TransformPattern unavailable or CanResize = false).");
                }
                transform.Resize(width, height);
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(string.Empty, "resize", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>
    /// ウィンドウを最小化する。UIA <c>WindowPattern.SetWindowVisualState(Minimized)</c>。
    /// Pattern 不在の場合は <see cref="ElementInteractionException"/>。
    /// </summary>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="ElementInteractionException">WindowPattern 不在 / 操作失敗の場合。</exception>
    public Task MinimizeAsync(CancellationToken ct = default)
        => SetWindowVisualStateAsync(WindowVisualState.Minimized, "minimize", ct);

    /// <summary>
    /// ウィンドウを最大化する。UIA <c>WindowPattern.SetWindowVisualState(Maximized)</c>。
    /// Pattern 不在の場合は <see cref="ElementInteractionException"/>。
    /// </summary>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="ElementInteractionException">WindowPattern 不在 / 操作失敗の場合。</exception>
    public Task MaximizeAsync(CancellationToken ct = default)
        => SetWindowVisualStateAsync(WindowVisualState.Maximized, "maximize", ct);

    /// <summary>
    /// ウィンドウを通常表示に復元する。UIA <c>WindowPattern.SetWindowVisualState(Normal)</c>。
    /// Pattern 不在の場合は <see cref="ElementInteractionException"/>。
    /// </summary>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="ElementInteractionException">WindowPattern 不在 / 操作失敗の場合。</exception>
    public Task RestoreAsync(CancellationToken ct = default)
        => SetWindowVisualStateAsync(WindowVisualState.Normal, "restore", ct);

    /// <summary>
    /// minimize / maximize / restore の共通実装。WindowPattern の <c>SetWindowVisualState</c> を呼ぶ。
    /// </summary>
    /// <param name="state">目標とする <see cref="WindowVisualState"/>。</param>
    /// <param name="opName">エラーメッセージ用のオペレーション名 (<c>minimize</c>/<c>maximize</c>/<c>restore</c>)。</param>
    /// <param name="ct">キャンセルトークン。</param>
    private Task SetWindowVisualStateAsync(WindowVisualState state, string opName, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                var windowPattern = _window.Patterns.Window.PatternOrDefault;
                if (windowPattern is null)
                {
                    throw new ElementInteractionException(string.Empty, opName,
                        "Window does not support WindowPattern.");
                }
                windowPattern.SetWindowVisualState(state);
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(string.Empty, opName, ex.Message, ex);
            }
            // auto-wait は WaitForInputIdle + 50ms sleep のみで座標非依存のため、minimize 後でも安全に呼べる。
            // snapshot 取得は呼び出し側 (CLI auto-snapshot) の責務であり、minimize 後の座標取得失敗は
            // CLI 層で警告扱いにする想定 (本メソッドでは関知しない)。
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }
}
