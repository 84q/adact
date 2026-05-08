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
    /// Clicks an element using the provided mouse options.
    /// </summary>
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
    /// Performs a double-click on an element.
    /// </summary>
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
    /// Moves the mouse over an element.
    /// </summary>
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
    /// Moves the mouse pointer to a point or element.
    /// </summary>
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
    /// Presses a mouse button at a point or element.
    /// </summary>
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
    /// Releases a mouse button at a point or element.
    /// </summary>
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
    /// Scrolls the mouse wheel at a point or element.
    /// </summary>
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

    private void PerformClick(IElement el, ClickOptions options, bool doubleclick)
    {
        var (x, y) = ComputeTargetPoint(el, options.PositionX, options.PositionY);
        var mods = ModifierKeys.Resolve(options.Modifiers);
        var btn = options.Button;

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

    private IDisposable PressModifiers(IReadOnlyList<VirtualKeyShort> modifiers)
    {
        if (modifiers.Count == 0) return NoopDisposable.Instance;
        foreach (var k in modifiers) _interaction.PressKey(k);
        return new ModifierReleaser(_interaction, modifiers, _logger);
    }

    private sealed class ModifierReleaser : IDisposable
    {
        private readonly IWindowInteractionDriver _interaction;
        private readonly IReadOnlyList<VirtualKeyShort> _keys;
        private readonly ILogger _logger;
        public ModifierReleaser(IWindowInteractionDriver interaction, IReadOnlyList<VirtualKeyShort> keys, ILogger logger)
        {
            _interaction = interaction;
            _keys = keys;
            _logger = logger;
        }
        /// <inheritdoc />
        public void Dispose()
        {
            for (int i = _keys.Count - 1; i >= 0; i--)
            {
                try { _interaction.ReleaseKey(_keys[i]); } catch (Exception ex) { _logger.LogTrace(ex, "ReleaseKey failed for {Key}", _keys[i]); }
            }
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();
        /// <inheritdoc />
        public void Dispose() { }
    }

    private static (int X, int Y) ComputeTargetPoint(IElement el, int? positionX, int? positionY)
    {
        var r = el.BoundingRectangle;
        int x = positionX is { } px ? r.X + px : r.X + r.Width / 2;
        int y = positionY is { } py ? r.Y + py : r.Y + r.Height / 2;
        return (x, y);
    }

    /// <summary>
    /// </summary>
    private (int X, int Y) ResolveTarget(MouseTarget target)
    {
        return target switch
        {
            MouseTarget.ByPoint p => (p.X, p.Y),
            MouseTarget.ByRef r => ComputeTargetPoint(_registry.Resolve(r.Ref), null, null),
            _ => throw new ArgumentException($"Unsupported MouseTarget: {target.GetType()}", nameof(target)),
        };
    }

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
