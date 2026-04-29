using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace Adact.Engine;

public sealed partial class WindowSession
{
    /// <summary>
    /// 指定要素を ToggleState.On にする。既に On なら何もしない。TogglePattern を持たない場合はエラー。
    /// </summary>
    /// <param name="refId">操作対象の Element Ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">refId が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">TogglePattern 不在 / 操作失敗の場合。</exception>
    public Task CheckAsync(string refId, CancellationToken ct = default)
        => SetToggleAsync(refId, ToggleState.On, "check", ct);

    /// <summary>
    /// 指定要素を ToggleState.Off にする。既に Off なら何もしない。TogglePattern を持たない場合はエラー。
    /// </summary>
    /// <param name="refId">操作対象の Element Ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">refId が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">TogglePattern 不在 / 操作失敗の場合。</exception>
    public Task UncheckAsync(string refId, CancellationToken ct = default)
        => SetToggleAsync(refId, ToggleState.Off, "uncheck", ct);

    /// <summary>
    /// 指定要素 (List / ComboBox 等) の選択肢を、name / index / item-ref のいずれかで選択する。
    /// ComboBox が closed の場合は事前に ExpandCollapsePattern.Expand を試行する。
    /// </summary>
    /// <param name="refId">List / ComboBox 等のコンテナ要素 Ref。</param>
    /// <param name="name">選択する子 ListItem の Name。</param>
    /// <param name="index">0-based での選択 index。</param>
    /// <param name="itemRef">snapshot で得た子 ListItem の Element Ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="ArgumentException">3 つの選択指定がいずれも未指定 / 複数指定の場合。</exception>
    /// <exception cref="RefNotFoundException">ref が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">SelectionItemPattern が無い / 子が見つからない場合。</exception>
    public Task SelectAsync(string refId, string? name, int? index, string? itemRef, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ValidateExactlyOne(name, index, itemRef);
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            var container = _registry.Resolve(refId);
            try
            {
                var inner = Inner(container);
                TryExpand(inner);

                AutomationElement? target;
                if (itemRef is not null)
                {
                    target = Inner(_registry.Resolve(itemRef));
                }
                else if (index is { } idx)
                {
                    var children = inner.FindAllChildren();
                    if (idx < 0 || idx >= children.Length)
                    {
                        throw new ElementInteractionException(refId, "select",
                            $"index {idx} is out of range (child count = {children.Length}).");
                    }
                    target = children[idx];
                }
                else
                {
                    var children = inner.FindAllChildren();
                    target = children.FirstOrDefault(e => string.Equals(
                        SafeName(e), name, StringComparison.Ordinal));
                    if (target is null)
                    {
                        throw new ElementInteractionException(refId, "select",
                            $"no child item matches name '{name}'.");
                    }
                }

                var sel = target.Patterns.SelectionItem.PatternOrDefault;
                if (sel is null)
                {
                    throw new ElementInteractionException(refId, "select",
                        "target item does not support SelectionItemPattern.");
                }
                sel.Select();
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "select", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>
    /// 指定要素にキーボードフォーカスを当てる。auto-snapshot は発火しない (補助系)。
    /// </summary>
    /// <param name="refId">フォーカス対象の Element Ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">refId が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">SetFocus が失敗した場合。</exception>
    public Task FocusAsync(string refId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            var el = _registry.Resolve(refId);
            try
            {
                el.Focus();
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "focus", ex.Message, ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>
    /// 指定入力要素を空文字で <see cref="FillAsync"/> し、内容をクリアする。
    /// </summary>
    /// <param name="refId">クリア対象の Element Ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">refId が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">UIA 操作が失敗した場合。</exception>
    public Task ClearAsync(string refId, CancellationToken ct = default) => FillAsync(refId, string.Empty, ct);

    /// <summary>
    /// 指定要素を ScrollItemPattern.ScrollIntoView で表示領域内に持ってくる。Pattern 不在時はエラー。
    /// </summary>
    /// <param name="refId">対象 Element Ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">refId が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">ScrollItemPattern 不在 / 操作失敗の場合。</exception>
    public Task ScrollIntoViewAsync(string refId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            var el = _registry.Resolve(refId);
            try
            {
                var inner = Inner(el);
                var pat = inner.Patterns.ScrollItem.PatternOrDefault;
                if (pat is null)
                {
                    throw new ElementInteractionException(refId, "scroll-into-view",
                        "element does not support ScrollItemPattern.");
                }
                pat.ScrollIntoView();
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "scroll-into-view", ex.Message, ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>Toggle 系の共通実装: 現在の <see cref="ToggleState"/> を読み、必要なら <c>Toggle()</c> を呼ぶ。</summary>
    /// <param name="refId">対象 Element Ref。</param>
    /// <param name="desired">目標とする <see cref="ToggleState"/>。</param>
    /// <param name="opName">エラーメッセージ用のオペレーション名 (<c>check</c>/<c>uncheck</c>)。</param>
    /// <param name="ct">キャンセルトークン。</param>
    private Task SetToggleAsync(string refId, ToggleState desired, string opName, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            var el = _registry.Resolve(refId);
            try
            {
                var inner = Inner(el);
                var toggle = inner.Patterns.Toggle.PatternOrDefault;
                if (toggle is null)
                {
                    // SelectionItemPattern (e.g. RadioButton) でも check/uncheck を実装する。
                    var sel = inner.Patterns.SelectionItem.PatternOrDefault;
                    if (sel is not null)
                    {
                        if (desired == ToggleState.On)
                        {
                            sel.Select();
                            return;
                        }
                        throw new ElementInteractionException(refId, opName,
                            "uncheck is not supported on SelectionItem-only elements.");
                    }
                    throw new ElementInteractionException(refId, opName,
                        "element does not support TogglePattern.");
                }

                var current = toggle.ToggleState.ValueOrDefault;
                if (current == desired) return;
                toggle.Toggle();
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, opName, ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>ComboBox 等が closed の場合に Expand を試行する (失敗は無視)。</summary>
    /// <param name="el">対象 UIA 要素。</param>
    private static void TryExpand(AutomationElement el)
    {
        try
        {
            var ec = el.Patterns.ExpandCollapse.PatternOrDefault;
            if (ec is null) return;
            var state = ec.ExpandCollapseState.ValueOrDefault;
            if (state == ExpandCollapseState.Collapsed)
            {
                ec.Expand();
            }
        }
        catch
        {
            // best effort
        }
    }

    /// <summary><see cref="IElement"/> から実体の <see cref="AutomationElement"/> を取り出す。</summary>
    /// <param name="el">UIA 要素ラッパ。</param>
    /// <returns>FlaUI <see cref="AutomationElement"/>。</returns>
    /// <exception cref="InvalidOperationException">FlaUI ラッパでない場合 (テスト用 FakeElement 等)。</exception>
    private static AutomationElement Inner(IElement el)
    {
        if (el is FlaUiElement fue) return fue.Inner;
        throw new InvalidOperationException(
            $"UIA pattern operation requires a FlaUiElement, got {el.GetType().Name}.");
    }

    /// <summary><see cref="AutomationElement.Properties"/> から Name を例外抑制で取得する。</summary>
    /// <param name="el">UIA 要素。</param>
    /// <returns>Name または空文字。</returns>
    private static string SafeName(AutomationElement el)
    {
        try { return el.Properties.Name.ValueOrDefault ?? string.Empty; }
        catch { return string.Empty; }
    }

    /// <summary>name / index / itemRef のうちちょうど 1 つだけ指定されていることを検証する。</summary>
    /// <param name="name">指定された Name。</param>
    /// <param name="index">指定された 0-based index。</param>
    /// <param name="itemRef">指定された子 Element Ref。</param>
    /// <exception cref="ArgumentException">指定数が 1 でない場合。</exception>
    private static void ValidateExactlyOne(string? name, int? index, string? itemRef)
    {
        int count = (name is not null ? 1 : 0) + (index.HasValue ? 1 : 0) + (itemRef is not null ? 1 : 0);
        if (count != 1)
        {
            throw new ArgumentException(
                "select requires exactly one of 'name', 'index' or 'itemRef'.",
                nameof(name));
        }
    }
}
