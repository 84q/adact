namespace Adact.Engine;

/// <summary>
/// Describes a wait-for query against element properties.
/// </summary>
/// <param name="Name">Exact name match.</param>
/// <param name="ControlType">Exact control type match.</param>
/// <param name="AutomationId">Exact AutomationId match.</param>
/// <param name="ClassName">Exact ClassName match.</param>
public sealed record WaitForElementQuery(
    string? Name = null,
    string? ControlType = null,
    string? AutomationId = null,
    string? ClassName = null)
{
    /// <summary>
    /// Gets whether at least one condition is set.
    /// </summary>
    public bool HasAnyCondition =>
        !string.IsNullOrEmpty(Name)
        || !string.IsNullOrEmpty(ControlType)
        || !string.IsNullOrEmpty(AutomationId)
        || !string.IsNullOrEmpty(ClassName);
}
