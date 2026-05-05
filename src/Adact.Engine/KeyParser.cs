using System;
using System.Collections.Generic;
using System.Globalization;

using FlaUI.Core.WindowsAPI;

namespace Adact.Engine;

/// <summary>
/// Playwright 流のキー指定 (<c>"Ctrl+Shift+E"</c>, <c>"Enter"</c>, <c>"F1"</c> 等) を
/// 修飾キー列 + メインキーに分解し、<see cref="VirtualKeyShort"/> に解決するヘルパー。
/// <c>press</c> / <c>key-down</c> / <c>key-up</c> コマンドの引数解析で使う。
/// </summary>
public static class KeyParser
{
    /// <summary>
    /// <c>"Ctrl+Shift+E"</c> 形式のキー記述をトークン化する。最後のトークンがメインキー、それ以外は修飾キー扱い。
    /// </summary>
    /// <param name="input"><c>+</c> 区切りのキー文字列。空白可、空文字 / null は <see cref="ArgumentException"/>。</param>
    /// <returns>修飾キー列とメインキーに変換した結果。</returns>
    /// <exception cref="ArgumentException">空・トークン不正・未知キーの場合。</exception>
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
    /// 単一キー (<c>"Enter"</c>, <c>"a"</c>, <c>"F1"</c> 等) を解決する。修飾キーとの組合せは許容しない。
    /// </summary>
    /// <param name="input">単一キー名。</param>
    /// <returns>対応する仮想キー。</returns>
    /// <exception cref="ArgumentException">空、複数トークン、または未知キーの場合。</exception>
    public static VirtualKeyShort ParseSingle(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("key must be a non-empty string.", nameof(input));
        }

        if (input.Contains('+', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"key '{input}' contains '+': key-down/key-up accept only a single key. Use 'press' for combinations.",
                nameof(input));
        }
        return ResolveKey(input.Trim());
    }

    /// <summary>修飾キー名 (<c>Shift</c>, <c>Ctrl</c>, <c>Alt</c>, <c>Meta</c>, <c>ControlOrMeta</c>) を解決する。</summary>
    /// <param name="name">トークン文字列。</param>
    /// <returns>対応する仮想キー。</returns>
    /// <exception cref="ArgumentException">未知の修飾キー名の場合。</exception>
    private static VirtualKeyShort ResolveModifier(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "shift" => VirtualKeyShort.SHIFT,
            "control" or "ctrl" => VirtualKeyShort.CONTROL,
            "alt" => VirtualKeyShort.ALT,
            "meta" => VirtualKeyShort.LWIN,
            "controlormeta" => VirtualKeyShort.CONTROL,
            _ => throw new ArgumentException(
                $"'{name}' is not a known modifier. Expected: Shift, Control (Ctrl), Alt, Meta, ControlOrMeta.",
                nameof(name)),
        };
    }

    /// <summary>
    /// メインキー名を <see cref="VirtualKeyShort"/> に解決する。文字 / 数字 / F1-F24 / 主要な特殊キーをサポート。
    /// </summary>
    /// <param name="name">キー名。</param>
    /// <returns>対応する仮想キー。</returns>
    /// <exception cref="ArgumentException">未知のキー名の場合。</exception>
    private static VirtualKeyShort ResolveKey(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("key token must not be empty.", nameof(name));
        }

        // 1 文字の英字 / 数字 → KEY_A..KEY_Z / KEY_0..KEY_9
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
            "meta" => VirtualKeyShort.LWIN,
            _ => throw new ArgumentException(
                $"Unknown key '{name}'. Supported: A-Z, 0-9, F1-F24, Enter, Tab, Escape, Space, Backspace, Delete, Insert, Home, End, PageUp, PageDown, Arrow{{Up,Down,Left,Right}}, modifiers.",
                nameof(name)),
        };
    }
}
