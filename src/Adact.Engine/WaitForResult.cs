namespace Adact.Engine;

/// <summary>
/// <see cref="WindowSession.WaitForRefAsync"/> / <see cref="WindowSession.WaitForQueryAsync"/> の戻り値。
/// </summary>
/// <param name="Ref">解決された element ref (検索条件モードでは見つかった要素の ref、
/// detached 状態で要素が見つからなかった場合は元の ref または null)。</param>
/// <param name="State">最終的に満たされた状態。</param>
public sealed record WaitForResult(string? Ref, WaitForState State);
