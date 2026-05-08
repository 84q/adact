using System.Threading;
using System.Threading.Tasks;

using Adact.Engine.Exceptions;

using FlaUI.Core.Input;

namespace Adact.Engine;

public sealed partial class WindowSession
{
    /// <summary>
    /// Sends a key press to the window or a specific element.
    /// </summary>
    public Task PressAsync(string key, string? refId = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        var (mods, main) = KeyParser.Parse(key);
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                if (refId is not null)
                {
                    var el = _registry.Resolve(refId);
                    el.Focus();
                }
                else
                {
                    _interaction.FocusWindow();
                }

                using (PressModifiers(mods))
                {
                    _interaction.TypeKey(main);
                }
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId ?? "<window>", "press", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }

    /// <summary>
    /// Sends a key-down event.
    /// </summary>
    public Task KeyDownAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        var vk = KeyParser.ParseSingle(key);
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                _interaction.PressKey(vk);
            }
            catch (Exception ex)
            {
                throw new ElementInteractionException("<window>", "keydown", ex.Message, ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>
    /// Sends a key-up event.
    /// </summary>
    public Task KeyUpAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        var vk = KeyParser.ParseSingle(key);
        return RunSerializedAsync(c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                _interaction.ReleaseKey(vk);
            }
            catch (Exception ex)
            {
                throw new ElementInteractionException("<window>", "keyup", ex.Message, ex);
            }
            return Task.CompletedTask;
        }, ct);
    }

    /// <summary>
    /// Types text into the window or a specific element.
    /// </summary>
    public Task TypeAsync(string? refId, string text, int delayMs = 0, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        return RunSerializedAsync(async c =>
        {
            c.ThrowIfCancellationRequested();
            try
            {
                if (refId is not null)
                {
                    var el = _registry.Resolve(refId);
                    el.Focus();
                }
                else
                {
                    _interaction.FocusWindow();
                }

                if (delayMs <= 0)
                {
                    _interaction.TypeText(text);
                }
                else
                {
                    foreach (var ch in text)
                    {
                        c.ThrowIfCancellationRequested();
                        _interaction.TypeChar(ch);
                        await Task.Delay(delayMs, c).ConfigureAwait(false);
                    }
                }
            }
            catch (AdactException) { throw; }
            catch (Exception ex)
            {
                throw new ElementInteractionException(refId ?? "<window>", "type", ex.Message, ex);
            }
            await AutoWaitAfterInteractionAsync(c).ConfigureAwait(false);
        }, ct);
    }
}
