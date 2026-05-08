using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;

using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

using Microsoft.Extensions.Logging;

namespace Adact.Engine;

public sealed partial class WindowSession
{
    /// <summary>
    /// 指定要素を <see cref="ClickOptions"/> に従って詳細クリックする。
    /// 修飾キー / Position / Count / Button いずれも未指定の標準ケースでは Invoke パターン経由のクリックパスに委譲する。
    /// </summary>
    /// <param name="refId">操作対象の Element Ref。</param>
    /// <param name="options">クリックオプション (null は既定値扱い)。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">refId が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">UIA / 物理クリック操作が失敗した場合。</exception>
    public Task ClickWithOptionsAsync(string refId, ClickOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            var el = _registry.Resolve(refId);
            try
            {
                _interaction.FocusWindow();
                PerformClick(el, options, doubleclick: options.Double);
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "click", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>
    /// 指定要素を OS のダブルクリック判定内で 2 回クリックする。修飾キー / 位置 / ボタン指定は <paramref name="options"/> に従う。
    /// </summary>
    /// <param name="refId">操作対象の Element Ref。</param>
    /// <param name="options">クリックオプション (null は既定値扱い)。<see cref="ClickOptions.Count"/> は無視される。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">refId が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">UIA / 物理クリック操作が失敗した場合。</exception>
    public Task DoubleClickAsync(string refId, ClickOptions? options = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            var el = _registry.Resolve(refId);
            try
            {
                _interaction.FocusWindow();
                PerformClick(el, options ?? new ClickOptions(), doubleclick: true);
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "doubleclick", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>
    /// 指定要素の中心 (or 指定位置) にマウスカーソルを移動する。修飾キーは移動中保持される。
    /// </summary>
    /// <param name="refId">操作対象の Element Ref。</param>
    /// <param name="modifiers">マウス移動中に押下したままにする修飾キー名。null/空は修飾なし。</param>
    /// <param name="positionX">要素の bounding rect 左上を基準とする X オフセット (px)。null は中央。</param>
    /// <param name="positionY">要素の bounding rect 左上を基準とする Y オフセット (px)。null は中央。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">refId が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">UIA / 物理操作が失敗した場合。</exception>
    public Task HoverAsync(string refId, IReadOnlyList<string>? modifiers = null,
        int? positionX = null, int? positionY = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            var el = _registry.Resolve(refId);
            try
            {
                var (x, y) = ComputeTargetPoint(el, positionX, positionY);
                var mods = ModifierKeys.Resolve(modifiers);
                using (PressModifiers(mods))
                {
                    _interaction.MoveTo(x, y);
                }
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "hover", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>
    /// マウスカーソルを指定 target (要素 ref または絶対座標) に移動する。
    /// </summary>
    /// <param name="target">移動先 (<see cref="MouseTarget.ByRef"/> または <see cref="MouseTarget.ByPoint"/>)。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">target が ByRef で解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">物理操作が失敗した場合。</exception>
    public Task MouseMoveAsync(MouseTarget target, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                var (x, y) = ResolveTarget(target);
                _interaction.MoveTo(x, y);
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(DescribeTarget(target), "mousemove", ex.Message, ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>
    /// 指定 target の位置でマウスボタンを押し下げ、解放しないままにする。<see cref="MouseUpAsync"/> と対で使う。
    /// </summary>
    /// <param name="target">押下位置 (要素 ref または絶対座標)。</param>
    /// <param name="button">押下するボタン。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">target が ByRef で解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">物理操作が失敗した場合。</exception>
    public Task MouseDownAsync(MouseTarget target, MouseButton button, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                var (x, y) = ResolveTarget(target);
                _interaction.MoveTo(x, y);
                _interaction.MouseDown(button);
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(DescribeTarget(target), "mousedown", ex.Message, ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>
    /// 指定 target の位置でマウスボタンを解放する。<see cref="MouseDownAsync"/> と対で使う。
    /// </summary>
    /// <param name="target">解放位置 (要素 ref または絶対座標)。</param>
    /// <param name="button">解放するボタン。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">target が ByRef で解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">物理操作が失敗した場合。</exception>
    public Task MouseUpAsync(MouseTarget target, MouseButton button, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                var (x, y) = ResolveTarget(target);
                _interaction.MoveTo(x, y);
                _interaction.MouseUp(button);
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(DescribeTarget(target), "mouseup", ex.Message, ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>
    /// 指定 target の位置でマウスホイールをスクロールする。
    /// 設計 §6: <paramref name="deltaX"/> 正値=右、<paramref name="deltaY"/> 正値=下。
    /// FlaUI / Win32 は逆 (上が正) のため内部で符号を反転する。単位は notch (≒ 1 line)。
    /// </summary>
    /// <param name="target">スクロール起点 (要素 ref または絶対座標)。</param>
    /// <param name="deltaX">水平スクロール量。正値で右スクロール。</param>
    /// <param name="deltaY">垂直スクロール量。正値で下スクロール。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">target が ByRef で解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">物理操作が失敗した場合。</exception>
    public Task MouseWheelAsync(MouseTarget target, int deltaX, int deltaY, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                var (x, y) = ResolveTarget(target);
                _interaction.MoveTo(x, y);
                if (deltaY != 0)
                {
                    // FlaUI: 正値 = up scroll. 設計の符号 (Playwright 流, 正 = down) のため反転する。
                    _interaction.Scroll(-deltaY);
                }
                if (deltaX != 0)
                {
                    _interaction.HorizontalScroll(deltaX);
                }
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(DescribeTarget(target), "mousewheel", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>要素のクリック動作 (修飾キー押下 + N 連打 or ダブルクリック) を実行する。</summary>
    /// <param name="el">クリック対象要素。</param>
    /// <param name="options">クリックオプション。</param>
    /// <param name="doubleclick">true の場合は OS ダブルクリック判定を意図して 1 回の <c>DoubleClick</c> を発火する。</param>
    private void PerformClick(IElement el, ClickOptions options, bool doubleclick)
    {
        var (x, y) = ComputeTargetPoint(el, options.PositionX, options.PositionY);
        var mods = ModifierKeys.Resolve(options.Modifiers);
        var btn = options.Button;

        // 標準ケース (修飾なし、位置指定なし、左ボタン、Count==1、!doubleclick) は
        // 既存の Invoke パターン経由クリックを維持する (Phase 2 互換)。
        if (!doubleclick && options.Count <= 1 && mods.Count == 0
            && options.PositionX is null && options.PositionY is null
            && options.Button == MouseButton.Left)
        {
            el.Click();
            return;
        }

        _interaction.MoveTo(x, y);
        using (PressModifiers(mods))
        {
            if (doubleclick)
            {
                _interaction.MouseDoubleClick(btn);
            }
            else
            {
                int count = options.Count <= 0 ? 1 : options.Count;
                for (int i = 0; i < count; i++)
                {
                    _interaction.MouseClick(btn);
                }
            }
        }
    }

    /// <summary>修飾キーを押下し、戻り値の <see cref="IDisposable"/> 解放時に解放するヘルパ。</summary>
    /// <param name="modifiers">押下する修飾キー列。</param>
    /// <returns>解放時に modifiers をすべて解放する <see cref="IDisposable"/>。</returns>
    private IDisposable PressModifiers(IReadOnlyList<VirtualKeyShort> modifiers)
    {
        if (modifiers.Count == 0) return NoopDisposable.Instance;
        foreach (var k in modifiers) _interaction.PressKey(k);
        return new ModifierReleaser(_interaction, modifiers, _logger);
    }

    /// <summary>解放時に押下中の修飾キーをすべて Release する <see cref="IDisposable"/>。</summary>
    private sealed class ModifierReleaser : IDisposable
    {
        /// <summary>キー操作境界。</summary>
        private readonly IWindowInteractionDriver _interaction;
        /// <summary>押下中の修飾キー列。</summary>
        private readonly IReadOnlyList<VirtualKeyShort> _keys;
        /// <summary>診断用ロガー。</summary>
        private readonly ILogger _logger;
        /// <summary>解放対象の修飾キー列を保持して構築する。</summary>
        /// <param name="interaction">キー操作境界。</param>
        /// <param name="keys">解放する修飾キー列。</param>
        /// <param name="logger">診断用ロガー。</param>
        public ModifierReleaser(IWindowInteractionDriver interaction, IReadOnlyList<VirtualKeyShort> keys, ILogger logger)
        {
            _interaction = interaction;
            _keys = keys;
            _logger = logger;
        }
        /// <inheritdoc />
        public void Dispose()
        {
            // 押下と逆順で解放する (Win32 ベストプラクティス)。
            for (int i = _keys.Count - 1; i >= 0; i--)
            {
                try { _interaction.ReleaseKey(_keys[i]); } catch (Exception ex) { _logger.LogTrace(ex, "ReleaseKey failed for {Key}", _keys[i]); }
            }
        }
    }

    /// <summary>何もしない <see cref="IDisposable"/>。</summary>
    private sealed class NoopDisposable : IDisposable
    {
        /// <summary>共有インスタンス。</summary>
        public static readonly NoopDisposable Instance = new();
        /// <inheritdoc />
        public void Dispose() { }
    }

    /// <summary>要素の bounding rect 左上 + position offset、または rect 中央のスクリーン座標を返す。</summary>
    /// <param name="el">対象要素。</param>
    /// <param name="positionX">左上基準 X オフセット。null は中央。</param>
    /// <param name="positionY">左上基準 Y オフセット。null は中央。</param>
    /// <returns>スクリーン座標 (X, Y)。</returns>
    private static (int X, int Y) ComputeTargetPoint(IElement el, int? positionX, int? positionY)
    {
        var r = el.BoundingRectangle;
        int x = positionX is { } px ? r.X + px : r.X + r.Width / 2;
        int y = positionY is { } py ? r.Y + py : r.Y + r.Height / 2;
        return (x, y);
    }

    /// <summary>
    /// <see cref="MouseTarget"/> をスクリーン座標に解決する。<see cref="MouseTarget.ByRef"/> の場合は要素中央を採用。
    /// </summary>
    /// <param name="target">対象。</param>
    /// <returns>スクリーン座標。</returns>
    private (int X, int Y) ResolveTarget(MouseTarget target)
    {
        return target switch
        {
            MouseTarget.ByPoint p => (p.X, p.Y),
            MouseTarget.ByRef r => ComputeTargetPoint(_registry.Resolve(r.Ref), null, null),
            _ => throw new ArgumentException($"Unsupported MouseTarget: {target.GetType()}", nameof(target)),
        };
    }

    /// <summary>エラーメッセージ用に <see cref="MouseTarget"/> を文字列化する。</summary>
    /// <param name="target">対象。</param>
    /// <returns>「ref=…」または「point=x,y」形式の説明文字列。</returns>
    private static string DescribeTarget(MouseTarget target)
    {
        return target switch
        {
            MouseTarget.ByRef r => r.Ref,
            MouseTarget.ByPoint p => $"{p.X},{p.Y}",
            _ => target.ToString() ?? "<unknown>",
        };
    }
}
