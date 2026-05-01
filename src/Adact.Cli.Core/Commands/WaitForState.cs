namespace Adact.Cli;

/// <summary>
/// <c>adact wait-for</c> が待機する要素状態。Playwright と揃えるため既定は <see cref="Visible"/>。
/// </summary>
public enum WaitForState
{
    /// <summary>UIA tree に要素が存在する。</summary>
    Attached,
    /// <summary>UIA tree から要素が消えた。</summary>
    Detached,
    /// <summary>要素が存在し、<c>IsOffscreen == false</c>。</summary>
    Visible,
    /// <summary>要素が存在し、<c>IsOffscreen == true</c>。</summary>
    Hidden,
    /// <summary>要素が存在し、<c>IsEnabled == true</c>。</summary>
    Enabled,
    /// <summary>要素が存在し、<c>IsEnabled == false</c>。</summary>
    Disabled,
}

/// <summary>
/// <see cref="WaitForState"/> 文字列パーサ。CLI / MCP どちらからも同じ判定を共有する。
/// </summary>
public static class WaitForStateParser
{
    /// <summary>"visible"/"hidden"/"attached"/"detached"/"enabled"/"disabled" を解析する (case-insensitive)。</summary>
    /// <param name="value">解析対象。null/空は false。</param>
    /// <param name="state">解析結果。</param>
    /// <returns>成功時 true。</returns>
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

    /// <summary>許容される state 値のカンマ区切り表記 (エラーメッセージ用)。</summary>
    public const string AllowedValues = "attached, detached, visible, hidden, enabled, disabled";

    /// <summary><see cref="WaitForState"/> をワイヤフォーマット (lower-case) に変換する。</summary>
    /// <param name="state">対象 state。</param>
    /// <returns>lower-case 文字列。</returns>
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
