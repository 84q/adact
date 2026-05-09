using System.ComponentModel;

using Adact.Engine;
using Adact.Mcp.Common.InputDrivers;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    /// <summary>
    /// Presses a key combination as a global input action.
    /// </summary>
    /// <param name="key">The key combination to press.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The tool result.</returns>
    [McpServerTool(Name = "adact_keypress")]
    [Description("Press a key combo such as 'Ctrl+C' or 'Enter'. This is a low-level global input operation and does not require a session.")]
    public async Task<CallToolResult> PressAsync(
        [Description("Key combo (e.g. 'Enter', 'F5', 'Ctrl+Shift+E').")]
        string key,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrEmpty(key))
            return ToolErrors.Error(ToolErrors.InvalidArgument, "key must be a non-empty string.");

        try
        {
            var (mods, main) = KeyParser.Parse(key);
            using (PressModifiers(mods))
            {
                _keyboardDriver.TypeKey(main);
            }
            return new CallToolResult { Content = [] };
        }
        catch (ArgumentException ex)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, ex.Message);
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_keypress"); }
    }

    /// <summary>
    /// Presses and holds a single key as a global input action.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The tool result.</returns>
    [McpServerTool(Name = "adact_keydown")]
    [Description("Press and hold a single key. Pair with adact_keyup to release. This is a low-level global input operation and does not require a session.")]
    public async Task<CallToolResult> KeyDownAsync(
        [Description("Single key name (e.g. 'Shift', 'A', 'F1'). Combinations with '+' are not allowed here.")]
        string key,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(key))
            return ToolErrors.Error(ToolErrors.InvalidArgument, "key must be a non-empty string.");

        try
        {
            _keyboardDriver.PressKey(KeyParser.ParseSingle(key));
            return new CallToolResult { Content = [] };
        }
        catch (ArgumentException ex)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, ex.Message);
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_keydown"); }
    }

    /// <summary>
    /// Releases a key previously held by <c>adact_keydown</c>.
    /// </summary>
    /// <param name="key">The key to release.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The tool result.</returns>
    [McpServerTool(Name = "adact_keyup")]
    [Description("Release a single key previously pressed by adact_keydown. This is a low-level global input operation and does not require a session.")]
    public async Task<CallToolResult> KeyUpAsync(
        [Description("Single key name (must match the one passed to adact_keydown).")]
        string key,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(key))
            return ToolErrors.Error(ToolErrors.InvalidArgument, "key must be a non-empty string.");

        try
        {
            _keyboardDriver.ReleaseKey(KeyParser.ParseSingle(key));
            return new CallToolResult { Content = [] };
        }
        catch (ArgumentException ex)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, ex.Message);
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_keyup"); }
    }

    /// <summary>
    /// Focuses an element and types text into it character by character.
    /// </summary>
    /// <param name="ref">The target element reference.</param>
    /// <param name="text">The text to type.</param>
    /// <param name="delayMs">The optional delay between characters in milliseconds.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The tool result.</returns>
    [McpServerTool(Name = "adact_type")]
    [Description("Focus the element and type the given text character by character. Use adact_fill for atomic value-pattern set.")]
    public async Task<CallToolResult> TypeAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent adact_snapshot.")]
        string @ref,
        [Description("Text to type.")]
        string text,
        [Description("Delay between characters in milliseconds. 0 (default) means no delay.")]
        int? delayMs = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var refError)) return refError!;
        if (text is null)
            return ToolErrors.Error(ToolErrors.InvalidArgument, "text must not be null.");
        if (delayMs is { } d && d < 0)
            return ToolErrors.Error(ToolErrors.InvalidArgument, "delayMs must be >= 0.");

        try
        {
            await session!.TypeAsync(@ref, text, delayMs ?? 0, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_type"); }
    }

    private ModifierReleaseScope PressModifiers(IReadOnlyList<FlaUI.Core.WindowsAPI.VirtualKeyShort> modifiers)
    {
        foreach (var k in modifiers) _keyboardDriver.PressKey(k);
        return new ModifierReleaseScope(_keyboardDriver, modifiers);
    }

    private sealed class ModifierReleaseScope(IKeyboardDriver driver, IReadOnlyList<FlaUI.Core.WindowsAPI.VirtualKeyShort> modifiers) : IDisposable
    {
        public void Dispose()
        {
            for (var i = modifiers.Count - 1; i >= 0; i--)
            {
                driver.ReleaseKey(modifiers[i]);
            }
        }
    }
}
