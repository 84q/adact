namespace Adact.Engine;

/// <summary>
/// <see cref="WindowSession.WaitForQueryAsync"/> の検索条件。
/// 指定されたフィールドはすべて exact match (case-insensitive) で AND 結合される。
/// 少なくとも 1 つのフィールドを設定する必要がある。
/// </summary>
/// <param name="Name">UIA Name プロパティ exact match。</param>
/// <param name="ControlType">UIA ControlType (例 "Button") exact match (case-insensitive)。</param>
/// <param name="AutomationId">AutomationId exact match。</param>
/// <param name="ClassName">ClassName exact match。</param>
public sealed record WaitForElementQuery(
    string? Name = null,
    string? ControlType = null,
    string? AutomationId = null,
    string? ClassName = null)
{
    /// <summary>少なくとも 1 つのフィールドが設定されているなら true。</summary>
    public bool HasAnyCondition =>
        !string.IsNullOrEmpty(Name)
        || !string.IsNullOrEmpty(ControlType)
        || !string.IsNullOrEmpty(AutomationId)
        || !string.IsNullOrEmpty(ClassName);
}
