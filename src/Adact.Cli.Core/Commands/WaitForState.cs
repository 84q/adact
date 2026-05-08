namespace Adact.Cli;

/// <summary>
/// </summary>
public enum WaitForState
{
    Attached,
    Detached,
    Visible,
    Hidden,
    Enabled,
    Disabled,
}

/// <summary>
/// </summary>
public static class WaitForStateParser
{
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

    public const string AllowedValues = "attached, detached, visible, hidden, enabled, disabled";

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
