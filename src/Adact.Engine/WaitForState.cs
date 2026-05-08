namespace Adact.Engine;

/// <summary>
/// Supported wait-for states.
/// </summary>
public enum WaitForState
{
    /// <summary>
    /// The element is attached.
    /// </summary>
    Attached,

    /// <summary>
    /// The element is detached.
    /// </summary>
    Detached,

    /// <summary>
    /// The element is visible.
    /// </summary>
    Visible,

    /// <summary>
    /// The element is hidden.
    /// </summary>
    Hidden,

    /// <summary>
    /// The element is enabled.
    /// </summary>
    Enabled,

    /// <summary>
    /// The element is disabled.
    /// </summary>
    Disabled,
}

/// <summary>
/// Parses and formats wait-for states.
/// </summary>
public static class WaitForStateParser
{
    /// <summary>
    /// Tries to parse a wait-for state from wire format.
    /// </summary>
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
    /// The allowed wire values for wait-for state parsing.
    /// </summary>
    public const string AllowedValues = "attached, detached, visible, hidden, enabled, disabled";

    /// <summary>
    /// Converts a state to its wire-format string.
    /// </summary>
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
