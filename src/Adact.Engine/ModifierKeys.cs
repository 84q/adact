using System.Collections.Generic;

using FlaUI.Core.WindowsAPI;

namespace Adact.Engine;

/// <summary>
/// </summary>
internal static class ModifierKeys
{
    /// <summary>
    /// </summary>
    public static IReadOnlyList<VirtualKeyShort> Resolve(IReadOnlyList<string>? names)
    {
        if (names is null || names.Count == 0)
        {
            return System.Array.Empty<VirtualKeyShort>();
        }

        var result = new List<VirtualKeyShort>(names.Count);
        foreach (var raw in names)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var key = ResolveOne(raw.Trim());
            if (!result.Contains(key)) result.Add(key);
        }
        return result;
    }

    private static VirtualKeyShort ResolveOne(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "shift" => VirtualKeyShort.SHIFT,
            "control" or "ctrl" => VirtualKeyShort.CONTROL,
            "alt" => VirtualKeyShort.ALT,
            "meta" or "win" or "windows" => VirtualKeyShort.LWIN,
            _ => throw new System.ArgumentException(
                $"Unknown modifier '{name}'. Expected one of: Shift, Control (Ctrl), Alt, Meta (Win, Windows).",
                nameof(name)),
        };
    }
}
