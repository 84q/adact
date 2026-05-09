namespace Adact.Cli;

/// <summary>
/// Represents a window or element state that a wait command can target.
/// </summary>
public enum WaitForState
{
    /// <summary>
    /// The target is attached.
    /// </summary>
    Attached,
    /// <summary>
    /// The target is detached.
    /// </summary>
    Detached,
    /// <summary>
    /// The target is visible.
    /// </summary>
    Visible,
    /// <summary>
    /// The target is hidden.
    /// </summary>
    Hidden,
    /// <summary>
    /// The target is enabled.
    /// </summary>
    Enabled,
    /// <summary>
    /// The target is disabled.
    /// </summary>
    Disabled,
}

/// <summary>
/// Parses <see cref="WaitForState"/> values used by CLI and wire payloads.
/// </summary>
public static class WaitForStateParser
{
    /// <summary>
    /// Attempts to parse a wait state value.
    /// </summary>
    /// <param name="value">The input value to parse.</param>
    /// <param name="state">The parsed state when successful.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? value, out WaitForState state)
    {
        state = WaitForState.Visible;
        if (string.IsNullOrEmpty(value)) return false;
        switch (value.Trim().ToLowerInvariant())
        {
            case "visible": state = WaitForState.Visible; return true;
            case "hidden": state = WaitForState.Hidden; return true;
            case "attached": state = WaitForState.Attached; return true;
            case "detached": state = WaitForState.Detached; return true;
            case "enabled": state = WaitForState.Enabled; return true;
            case "disabled": state = WaitForState.Disabled; return true;
            default: return false;
        }
    }

    /// <summary>
    /// Gets the comma-separated list of accepted wait state values.
    /// </summary>
    public const string AllowedValues = "attached, detached, visible, hidden, enabled, disabled";

    /// <summary>
    /// Converts a wait state to its wire-format string.
    /// </summary>
    /// <param name="state">The state to convert.</param>
    /// <returns>The lowercase wire-format value.</returns>
    public static string ToWireString(WaitForState state) => state switch
    {
        WaitForState.Attached => "attached",
        WaitForState.Detached => "detached",
        WaitForState.Visible => "visible",
        WaitForState.Hidden => "hidden",
        WaitForState.Enabled => "enabled",
        WaitForState.Disabled => "disabled",
        _ => state.ToString().ToLowerInvariant(),
    };
}
