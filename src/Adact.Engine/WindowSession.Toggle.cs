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
    /// Checks an element.
    /// </summary>
    public Task CheckAsync(string refId, CancellationToken ct = default)
        => SetToggleAsync(refId, ToggleState.On, "check", ct);

    /// <summary>
    /// Unchecks an element.
    /// </summary>
    public Task UncheckAsync(string refId, CancellationToken ct = default)
        => SetToggleAsync(refId, ToggleState.Off, "uncheck", ct);

    /// <summary>
    /// Selects child items within a container element.
    /// </summary>
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
    /// Focuses an element.
    /// </summary>
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
    /// Scrolls an element into view.
    /// </summary>
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
    /// Scrolls an element using the specified mode.
    /// </summary>
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

    /// <returns>FlaUI <see cref="AutomationElement"/>。</returns>
    private static AutomationElement Inner(IElement el)
    {
        if (el is FlaUiElement fue) return fue.Inner;
        throw new InvalidOperationException(
            $"UIA pattern operation requires a FlaUiElement, got {el.GetType().Name}.");
    }

    private string SafeName(AutomationElement el)
    {
        try { return el.Properties.Name.ValueOrDefault ?? string.Empty; }
        catch (Exception ex) { _logger.LogTrace(ex, "Failed to get element Name"); return string.Empty; }
    }

}
