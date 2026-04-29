using System.CommandLine;
using System.Globalization;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// Phase 8 で追加した Mouse / Keyboard / Toggle 系コマンドの共通 Option ビルダ。
/// </summary>
internal static class OperationOptions
{
    /// <summary>auto-snapshot 抑制 Option。</summary>
    /// <returns>--no-snapshot Option。</returns>
    public static Option<bool> NoSnapshot() =>
        new("--no-snapshot") { Description = "Do not capture a snapshot after the action." };

    /// <summary>snapshot 出力ディレクトリ Option。</summary>
    /// <returns>--snapshot-dir Option。</returns>
    public static Option<string?> SnapshotDir() =>
        new("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };

    /// <summary>--button オプション (left/right/middle)。</summary>
    /// <returns>--button Option。</returns>
    public static Option<string?> Button() =>
        new("--button") { Description = "Mouse button: 'left' (default), 'right', or 'middle'." };

    /// <summary>--count オプション (連打回数)。</summary>
    /// <returns>--count Option。</returns>
    public static Option<int?> Count() =>
        new("--count") { Description = "Number of consecutive clicks (>= 1). Defaults to 1." };

    /// <summary>--modifier オプション (複数指定可)。</summary>
    /// <returns>--modifier Option。</returns>
    public static Option<string[]> Modifiers()
    {
        var opt = new Option<string[]>("--modifier")
        {
            Description = "Modifier key held during the action (Shift/Control/Ctrl/Alt/Meta/ControlOrMeta). Can be specified multiple times.",
            AllowMultipleArgumentsPerToken = true,
        };
        return opt;
    }

    /// <summary>--position "x,y" を解釈する。null なら out 引数は両方 null。失敗時は false。</summary>
    /// <param name="value">入力 (例 "20,30")。</param>
    /// <param name="x">解析結果 X。</param>
    /// <param name="y">解析結果 Y。</param>
    /// <returns>解析できれば true。null の場合も true (両方 null)。</returns>
    public static bool TryParsePosition(string? value, out int? x, out int? y)
    {
        x = null;
        y = null;
        if (string.IsNullOrEmpty(value)) return true;
        var parts = value.Split(',');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var px))
            return false;
        if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var py))
            return false;
        x = px;
        y = py;
        return true;
    }

    /// <summary>"left"/"right"/"middle" のいずれかを検証する。null/空は OK。</summary>
    /// <param name="button">--button 引数値。</param>
    /// <param name="error">エラーメッセージ (失敗時のみ)。</param>
    /// <returns>OK なら true。</returns>
    public static bool ValidateButton(string? button, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrEmpty(button)) return true;
        switch (button.Trim().ToLowerInvariant())
        {
            case "left":
            case "right":
            case "middle":
                return true;
            default:
                error = $"--button must be one of 'left', 'right', 'middle' (got '{button}').";
                return false;
        }
    }

    /// <summary>修飾キー名のセットを検証する。</summary>
    /// <param name="modifiers">入力配列。null/空は許容。</param>
    /// <param name="error">エラーメッセージ。</param>
    /// <returns>OK なら true。</returns>
    public static bool ValidateModifiers(IReadOnlyList<string>? modifiers, out string error)
    {
        error = string.Empty;
        if (modifiers is null) return true;
        foreach (var m in modifiers)
        {
            switch (m?.Trim().ToLowerInvariant())
            {
                case "shift":
                case "control":
                case "ctrl":
                case "alt":
                case "meta":
                case "controlormeta":
                    continue;
                default:
                    error = $"Unknown modifier '{m}'. Allowed: Shift, Control, Ctrl, Alt, Meta, ControlOrMeta.";
                    return false;
            }
        }
        return true;
    }

    /// <summary>共通的な引数検証エラーを stderr に出力して User error コードを返す。</summary>
    /// <param name="message">メッセージ。</param>
    /// <returns><see cref="ExitCodes.UserError"/>。</returns>
    public static int ReportUserError(string message)
    {
        CliError.Write(ErrorCodes.InvalidArgument, message);
        return ExitCodes.UserError;
    }
}
