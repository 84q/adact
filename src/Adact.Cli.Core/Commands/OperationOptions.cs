using System.CommandLine;
using System.Globalization;

using Adact.Cli.Output;

namespace Adact.Cli.Commands;

/// <summary>
/// </summary>
internal static class OperationOptions
{
    /// <returns>--no-snapshot Option。</returns>
    public static Option<bool> NoSnapshot() =>
        new("--no-snapshot") { Description = "Do not capture a snapshot after the action." };

    /// <returns>--snapshot-dir Option。</returns>
    public static Option<string?> SnapshotDir() =>
        new("--snapshot-dir") { Description = "Snapshot output directory (default '.adact/')." };

    /// <returns>--button Option。</returns>
    public static Option<string?> Button() =>
        new("--button") { Description = "Mouse button: 'left' (default), 'right', or 'middle'." };

    /// <returns>--count Option。</returns>
    public static Option<int?> Count() =>
        new("--count") { Description = "Number of consecutive clicks (>= 1). Defaults to 1." };

    /// <returns>--modifier Option。</returns>
    public static Option<string[]> Modifiers()
    {
        var opt = new Option<string[]>("--modifier")
        {
            Description = "Modifier key held during the action (Shift/Control/Ctrl/Alt/Meta/Win/Windows). Can be specified multiple times.",
            AllowMultipleArgumentsPerToken = true,
        };
        return opt;
    }

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
                case "win":
                case "windows":
                    continue;
                default:
                    error = $"Unknown modifier '{m}'. Allowed: Shift, Control, Ctrl, Alt, Meta, Win, Windows.";
                    return false;
            }
        }
        return true;
    }

    /// <returns><see cref="ExitCodes.UserError"/>。</returns>
    public static int ReportUserError(string message)
    {
        CliError.Write(ErrorCodes.InvalidArgument, message);
        return ExitCodes.UserError;
    }
}
