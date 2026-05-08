using System;
using System.Collections.Generic;
using System.Globalization;

using FlaUI.Core.WindowsAPI;

namespace Adact.Engine;

/// <summary>
/// </summary>
public static class KeyParser
{
    /// <summary>
    /// </summary>
    public static (IReadOnlyList<VirtualKeyShort> Modifiers, VirtualKeyShort MainKey) Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("key must be a non-empty string.", nameof(input));
        }

        var tokens = input.Split('+', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw new ArgumentException($"key '{input}' did not contain any tokens.", nameof(input));
        }

        var modifiers = new List<VirtualKeyShort>(tokens.Length - 1);
        for (int i = 0; i < tokens.Length - 1; i++)
        {
            modifiers.Add(ResolveModifier(tokens[i].Trim()));
        }
        var main = ResolveKey(tokens[^1].Trim());
        return (modifiers, main);
    }

    /// <summary>
    /// </summary>
    public static VirtualKeyShort ParseSingle(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("key must be a non-empty string.", nameof(input));
        }

        if (input.Contains('+', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"key '{input}' contains '+': keydown/keyup accept only a single key. Use 'keypress' for combinations.",
                nameof(input));
        }
        return ResolveKey(input.Trim());
    }

    private static VirtualKeyShort ResolveModifier(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "shift" => VirtualKeyShort.SHIFT,
            "control" or "ctrl" => VirtualKeyShort.CONTROL,
            "alt" => VirtualKeyShort.ALT,
            "meta" or "win" or "windows" => VirtualKeyShort.LWIN,
            _ => throw new ArgumentException(
                $"'{name}' is not a known modifier. Expected: Shift, Control (Ctrl), Alt, Meta (Win, Windows).",
                nameof(name)),
        };
    }

    /// <summary>
    /// </summary>
    private static VirtualKeyShort ResolveKey(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("key token must not be empty.", nameof(name));
        }

        if (name.Length == 1)
        {
            char c = name[0];
            if (c is >= 'a' and <= 'z') c = (char)(c - 'a' + 'A');
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                return Enum.Parse<VirtualKeyShort>(
                    "KEY_" + c.ToString(CultureInfo.InvariantCulture));
            }
        }

        // F1..F24
        if ((name.Length == 2 || name.Length == 3)
            && (name[0] == 'F' || name[0] == 'f')
            && int.TryParse(name.AsSpan(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var fn)
            && fn is >= 1 and <= 24)
        {
            return Enum.Parse<VirtualKeyShort>(
                "F" + fn.ToString(CultureInfo.InvariantCulture));
        }

        return name.ToLowerInvariant() switch
        {
            "enter" or "return" => VirtualKeyShort.RETURN,
            "tab" => VirtualKeyShort.TAB,
            "escape" or "esc" => VirtualKeyShort.ESCAPE,
            "space" => VirtualKeyShort.SPACE,
            "backspace" or "back" => VirtualKeyShort.BACK,
            "delete" or "del" => VirtualKeyShort.DELETE,
            "insert" or "ins" => VirtualKeyShort.INSERT,
            "home" => VirtualKeyShort.HOME,
            "end" => VirtualKeyShort.END,
            "pageup" or "pgup" => VirtualKeyShort.PRIOR,
            "pagedown" or "pgdn" => VirtualKeyShort.NEXT,
            "arrowup" or "up" => VirtualKeyShort.UP,
            "arrowdown" or "down" => VirtualKeyShort.DOWN,
            "arrowleft" or "left" => VirtualKeyShort.LEFT,
            "arrowright" or "right" => VirtualKeyShort.RIGHT,
            "shift" => VirtualKeyShort.SHIFT,
            "control" or "ctrl" => VirtualKeyShort.CONTROL,
            "alt" => VirtualKeyShort.ALT,
            "meta" or "win" or "windows" => VirtualKeyShort.LWIN,
            _ => throw new ArgumentException(
                $"Unknown key '{name}'. Supported: A-Z, 0-9, F1-F24, Enter, Tab, Escape, Space, Backspace, Delete, Insert, Home, End, PageUp, PageDown, Arrow{{Up,Down,Left,Right}}, modifiers.",
                nameof(name)),
        };
    }
}
