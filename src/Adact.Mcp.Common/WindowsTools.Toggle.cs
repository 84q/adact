using System.ComponentModel;

using Adact.Engine.Snapshot;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    /// <summary>Toggle 系要素を On 状態にする (既に On なら何もしない)。</summary>
    /// <param name="ref">対象 element ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "adact_check")]
    [Description("Ensure a checkbox / toggle / radio is in the On (selected) state. Idempotent.")]
    public async Task<CallToolResult> CheckAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent adact_snapshot.")]
        string @ref,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var error)) return error!;
        try
        {
            await session!.CheckAsync(@ref, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_check"); }
    }

    /// <summary>Toggle 系要素を Off 状態にする (既に Off なら何もしない)。</summary>
    /// <param name="ref">対象 element ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "adact_uncheck")]
    [Description("Ensure a checkbox / toggle is in the Off (unselected) state. Idempotent.")]
    public async Task<CallToolResult> UncheckAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent adact_snapshot.")]
        string @ref,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var error)) return error!;
        try
        {
            await session!.UncheckAsync(@ref, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_uncheck"); }
    }

    /// <summary>List / ComboBox 等の選択肢を name / index / itemRef のいずれかで選ぶ。</summary>
    /// <param name="ref">コンテナ要素 ref。</param>
    /// <param name="name">選択する子の Name。</param>
    /// <param name="index">0-based 子インデックス。</param>
    /// <param name="itemRef">子 ListItem の element ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。3 つの選択指定が 0 個または 2 個以上のときは <c>INVALID_ARGUMENT</c>。</returns>
    [McpServerTool(Name = "adact_select")]
    [Description("Select an item in a list/combobox by its Name ('name'), 0-based 'index', or child 'itemRef'. Provide exactly one.")]
    public async Task<CallToolResult> SelectAsync(
        [Description("Ref ID of the container (List, ComboBox, etc.).")]
        string @ref,
        [Description("Name of the child item to select.")]
        string? name = null,
        [Description("0-based index of the child item to select.")]
        int? index = null,
        [Description("Element ref of the child ListItem to select (from a recent snapshot).")]
        string? itemRef = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var error)) return error!;

        int specified = (name is not null ? 1 : 0) + (index.HasValue ? 1 : 0) + (itemRef is not null ? 1 : 0);
        if (specified != 1)
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "select requires exactly one of 'name', 'index', or 'itemRef'.");

        if (itemRef is not null && !RefId.TryParse(itemRef, out _, out _))
            return ToolErrors.Error(ToolErrors.InvalidRefFormat, $"Ref ID '{itemRef}' is malformed.");

        try
        {
            await session!.SelectAsync(@ref, name, index, itemRef, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_select"); }
    }

    /// <summary>指定要素にキーボードフォーカスを当てる。auto-snapshot は実行しない。</summary>
    /// <param name="ref">対象 element ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "adact_focus")]
    [Description("Set keyboard focus to the element identified by ref.")]
    public async Task<CallToolResult> FocusAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent adact_snapshot.")]
        string @ref,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var error)) return error!;
        try
        {
            await session!.FocusAsync(@ref, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_focus"); }
    }

    /// <summary>ScrollItemPattern で要素を表示領域内へスクロールする。</summary>
    /// <param name="ref">対象 element ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。Pattern 不在は <c>ELEMENT_INTERACTION_FAILED</c>。</returns>
    [McpServerTool(Name = "adact_scroll_into_view")]
    [Description("Scroll the element into view using ScrollItemPattern. Errors if the element does not support the pattern.")]
    public async Task<CallToolResult> ScrollIntoViewAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent adact_snapshot.")]
        string @ref,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var error)) return error!;
        try
        {
            await session!.ScrollIntoViewAsync(@ref, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_scroll_into_view"); }
    }

    /// <summary>ScrollPattern でコンテナ要素をスクロールする。percent / small / large は排他。</summary>
    /// <param name="ref">対象コンテナ要素の ref。</param>
    /// <param name="percentH">水平スクロール位置 (0〜100)。</param>
    /// <param name="percentV">垂直スクロール位置 (0〜100)。</param>
    /// <param name="smallH">水平 SmallIncrement/Decrement 回数。正=右、負=左。</param>
    /// <param name="smallV">垂直 SmallIncrement/Decrement 回数。正=下、負=上。</param>
    /// <param name="largeH">水平 LargeIncrement/Decrement 回数。正=右、負=左。</param>
    /// <param name="largeV">垂直 LargeIncrement/Decrement 回数。正=下、負=上。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。Pattern 不在は <c>ELEMENT_INTERACTION_FAILED</c>。排他違反は <c>INVALID_ARGUMENT</c>。</returns>
    [McpServerTool(Name = "adact_scroll")]
    [Description("Scroll a container element using ScrollPattern. Specify exactly one group: percent (percentH/percentV), small (smallH/smallV), or large (largeH/largeV).")]
    public async Task<CallToolResult> ScrollAsync(
        [Description("Ref ID of the scrollable container element.")]
        string @ref,
        [Description("Horizontal scroll position in percent (0-100). Use with percentV.")]
        int? percentH = null,
        [Description("Vertical scroll position in percent (0-100). Use with percentV.")]
        int? percentV = null,
        [Description("Number of small horizontal scrolls. Positive=right, negative=left.")]
        int? smallH = null,
        [Description("Number of small vertical scrolls. Positive=down, negative=up.")]
        int? smallV = null,
        [Description("Number of large horizontal scrolls. Positive=right, negative=left.")]
        int? largeH = null,
        [Description("Number of large vertical scrolls. Positive=down, negative=up.")]
        int? largeV = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var error)) return error!;

        bool hasPercent = percentH is not null || percentV is not null;
        bool hasSmall = smallH is not null || smallV is not null;
        bool hasLarge = largeH is not null || largeV is not null;
        int groupCount = (hasPercent ? 1 : 0) + (hasSmall ? 1 : 0) + (hasLarge ? 1 : 0);

        if (groupCount == 0)
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "At least one scroll parameter must be specified (percentH/percentV, smallH/smallV, or largeH/largeV).");
        if (groupCount > 1)
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "percent, small, and large groups are mutually exclusive. Specify only one group.");

        Adact.Engine.ScrollMode mode = (hasPercent, hasSmall, hasLarge) switch
        {
            (true, _, _) => new Adact.Engine.PercentMode(percentH, percentV),
            (_, true, _) => new Adact.Engine.SmallMode(smallH, smallV),
            _ => new Adact.Engine.LargeMode(largeH, largeV),
        };

        try
        {
            await session!.ScrollAsync(@ref, mode, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_scroll"); }
    }
}
