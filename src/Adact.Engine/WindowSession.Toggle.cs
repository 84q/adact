using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Patterns;

using Microsoft.Extensions.Logging;

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
    /// 指定要素 (List / ComboBox 等) の選択肢を、SelectionTarget 配列で選択する。
    /// ComboBox が closed の場合は事前に ExpandCollapsePattern.Expand を試行する。
    /// </summary>
    /// <param name="refId">List / ComboBox 等のコンテナ要素 Ref。</param>
    /// <param name="targets">選択対象のアイテム配列。</param>
    /// <param name="mode">選択モード (Replace / Add / Remove)。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="ArgumentException">targets が空の場合。</exception>
    /// <exception cref="RefNotFoundException">ref が解決できない場合。</exception>
    /// <exception cref="InvalidOperationException">Add/Remove モードで CanSelectMultiple=false の場合。</exception>
    /// <exception cref="ElementInteractionException">SelectionItemPattern が無い / 子が見つからない場合。</exception>
    public Task SelectAsync(string refId, SelectionTarget[] targets, SelectionMode mode = SelectionMode.Replace, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(targets);
        if (targets.Length == 0)
            throw new ArgumentException("At least one selection target must be specified.", nameof(targets));
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            var container = _registry.Resolve(refId);
            try
            {
                if (container is ISelectableElement selectable)
                {
                    selectable.SelectItems(targets, mode);
                    return;
                }

                var inner = Inner(container);
                TryExpand(inner);

                // Add/Remove モードでは CanSelectMultiple を事前チェック
                if (mode is SelectionMode.Add or SelectionMode.Remove)
                {
                    var selectionPattern = inner.Patterns.Selection.PatternOrDefault;
                    if (selectionPattern is not null && !selectionPattern.CanSelectMultiple.ValueOrDefault)
                    {
                        throw new InvalidOperationException(
                            "The control does not support multiple selection (CanSelectMultiple=false).");
                    }
                }

                for (int i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    AutomationElement? targetElement = ResolveTargetElement(refId, inner, target);

                    var sel = targetElement.Patterns.SelectionItem.PatternOrDefault;
                    if (sel is null)
                    {
                        throw new ElementInteractionException(refId, "select",
                            "target item does not support SelectionItemPattern.");
                    }

                    switch (mode)
                    {
                        case SelectionMode.Replace:
                            if (i == 0)
                                sel.Select();
                            else
                                sel.AddToSelection();
                            break;
                        case SelectionMode.Add:
                            sel.AddToSelection();
                            break;
                        case SelectionMode.Remove:
                            sel.RemoveFromSelection();
                            break;
                    }
                }
            }
            catch (AdactException) { throw; }
            catch (InvalidOperationException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "select", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>SelectionTarget から UIA AutomationElement を解決する。</summary>
    private AutomationElement ResolveTargetElement(string refId, AutomationElement inner, SelectionTarget target)
    {
        return target switch
        {
            SelectionTarget.ByItemRef byRef => Inner(_registry.Resolve(byRef.ItemRef)),
            SelectionTarget.ByIndex byIdx => ResolveByIndex(refId, inner, byIdx.Index),
            SelectionTarget.ByName byName => ResolveByName(refId, inner, byName.Name),
            _ => throw new ArgumentException($"Unknown SelectionTarget type: {target.GetType().Name}", nameof(target)),
        };
    }

    private static AutomationElement ResolveByIndex(string refId, AutomationElement inner, int idx)
    {
        var children = inner.FindAllChildren();
        if (idx < 0 || idx >= children.Length)
        {
            throw new ElementInteractionException(refId, "select",
                $"index {idx} is out of range (child count = {children.Length}).");
        }
        return children[idx];
    }

    private AutomationElement ResolveByName(string refId, AutomationElement inner, string name)
    {
        var children = inner.FindAllChildren();
        var found = children.FirstOrDefault(e => string.Equals(
            SafeName(e), name, StringComparison.Ordinal));
        if (found is null)
        {
            throw new ElementInteractionException(refId, "select",
                $"no child item matches name '{name}'.");
        }
        return found;
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
                if (el is IScrollableElement scrollable)
                {
                    scrollable.ScrollIntoView();
                    return Task.CompletedTask;
                }

                var inner = Inner(el);
                var pat = inner.Patterns.ScrollItem.PatternOrDefault;
                if (pat is null)
                {
                    throw new ElementInteractionException(refId, "scroll",
                        "element does not support ScrollItemPattern.");
                }
                pat.ScrollIntoView();
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "scroll", ex.Message, ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>
    /// ScrollPattern を使ってコンテナ要素をスクロールする。
    /// </summary>
    /// <param name="refId">対象コンテナの Element Ref。</param>
    /// <param name="mode">スクロールモード (Percent / Small / Large)。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <exception cref="ObjectDisposedException">本セッションが Dispose 済みの場合。</exception>
    /// <exception cref="RefNotFoundException">refId が解決できない場合。</exception>
    /// <exception cref="ElementInteractionException">ScrollPattern 不在 / 操作失敗の場合。</exception>
    public Task ScrollAsync(string refId, ScrollMode mode, CancellationToken ct = default)
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
                var pat = inner.Patterns.Scroll.PatternOrDefault;
                if (pat is null)
                {
                    throw new ElementInteractionException(refId, "scroll",
                        "element does not support ScrollPattern.");
                }

                switch (mode)
                {
                    case PercentMode(var h, var v):
                        pat.SetScrollPercent(h ?? -1, v ?? -1);
                        break;

                    case SmallMode(var dh, var dv):
                        ScrollByAmount(pat, dh, dv, small: true);
                        break;

                    case LargeMode(var dh, var dv):
                        ScrollByAmount(pat, dh, dv, small: false);
                        break;
                }
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId, "scroll", ex.Message, ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>Small/Large スクロールを |delta| 回繰り返す共通ヘルパー。</summary>
    private static void ScrollByAmount(
        FlaUI.Core.Patterns.IScrollPattern pat,
        int? deltaH,
        int? deltaV,
        bool small)
    {
        var hAmount = ScrollAmount.NoAmount;
        var vAmount = ScrollAmount.NoAmount;

        // Vertical
        if (deltaV is { } dv && dv != 0)
        {
            vAmount = dv > 0
                ? (small ? ScrollAmount.SmallIncrement : ScrollAmount.LargeIncrement)
                : (small ? ScrollAmount.SmallDecrement : ScrollAmount.LargeDecrement);
            for (var i = 0; i < Math.Abs(dv); i++)
                pat.Scroll(ScrollAmount.NoAmount, vAmount);
        }

        // Horizontal
        if (deltaH is { } dh && dh != 0)
        {
            hAmount = dh > 0
                ? (small ? ScrollAmount.SmallIncrement : ScrollAmount.LargeIncrement)
                : (small ? ScrollAmount.SmallDecrement : ScrollAmount.LargeDecrement);
            for (var i = 0; i < Math.Abs(dh); i++)
                pat.Scroll(hAmount, ScrollAmount.NoAmount);
        }
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
                if (el is ICheckableElement checkable)
                {
                    var desiredChecked = desired == ToggleState.On;
                    if (checkable.IsChecked != desiredChecked)
                    {
                        checkable.SetChecked(desiredChecked);
                    }
                    return;
                }

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
    private void TryExpand(AutomationElement el)
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
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "ExpandCollapse pattern failed for element");
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
    private string SafeName(AutomationElement el)
    {
        try { return el.Properties.Name.ValueOrDefault ?? string.Empty; }
        catch (Exception ex) { _logger.LogTrace(ex, "Failed to get element Name"); return string.Empty; }
    }

}
