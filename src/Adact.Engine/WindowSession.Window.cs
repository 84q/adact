using Adact.Engine.Exceptions;

using FlaUI.Core.Definitions;

namespace Adact.Engine;

public sealed partial class WindowSession
{
    /// <summary>
    /// Resizes the attached window.
    /// </summary>
    public Task ResizeAsync(int? width, int? height, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (width is null && height is null)
            throw new ArgumentException("At least one of width or height must be specified.");
        if (width is <= 0) throw new ArgumentOutOfRangeException(nameof(width), "width must be > 0.");
        if (height is <= 0) throw new ArgumentOutOfRangeException(nameof(height), "height must be > 0.");
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
                var rect = _window.Properties.BoundingRectangle.ValueOrDefault;
                var w = width ?? (int)rect.Width;
                var h = height ?? (int)rect.Height;
                transform.Resize(w, h);
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
    /// Minimizes the attached window.
    /// </summary>
    public Task MinimizeAsync(CancellationToken ct = default)
        => SetWindowVisualStateAsync(WindowVisualState.Minimized, "minimize", ct);

    /// <summary>
    /// Maximizes the attached window.
    /// </summary>
    public Task MaximizeAsync(CancellationToken ct = default)
        => SetWindowVisualStateAsync(WindowVisualState.Maximized, "maximize", ct);

    /// <summary>
    /// Restores the attached window.
    /// </summary>
    public Task RestoreAsync(CancellationToken ct = default)
        => SetWindowVisualStateAsync(WindowVisualState.Normal, "restore", ct);

    /// <summary>
    /// </summary>
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
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }
}
