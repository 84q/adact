using System.Collections.Generic;

using FlaUI.Core.WindowsAPI;

namespace Adact.Engine;

/// <summary>
/// click / hover / doubleclick の <c>--modifier</c> 引数 (Playwright 流) を <see cref="VirtualKeyShort"/> 列に変換するヘルパー。
/// 受理する修飾キー名: <c>Shift</c>, <c>Control</c> (alias <c>Ctrl</c>), <c>Alt</c>, <c>Meta</c> (alias <c>Win</c>, <c>Windows</c>)。
/// </summary>
internal static class ModifierKeys
{
    /// <summary>
    /// 修飾キー名のリストを <see cref="VirtualKeyShort"/> 列に変換する。重複は除去される。
    /// </summary>
    /// <param name="names">修飾キー名のリスト (大文字小文字は無視)。<c>null</c> または空は空配列を返す。</param>
    /// <returns>UIA に渡せる <see cref="VirtualKeyShort"/> 配列。</returns>
    /// <exception cref="System.ArgumentException">未知の修飾キー名が含まれていた場合。</exception>
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

    /// <summary>1 個の修飾キー名を <see cref="VirtualKeyShort"/> に解決する。</summary>
    /// <param name="name">修飾キー名。</param>
    /// <returns>対応する仮想キー。</returns>
    /// <exception cref="System.ArgumentException">未知の修飾キー名の場合。</exception>
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
