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

    /// <summary>入力要素の内容を空文字列でクリアする (<c>FillAsync(ref, "")</c>)。</summary>
    /// <param name="ref">対象 element ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "adact_clear")]
    [Description("Clear the value of an input element (equivalent to adact_fill with an empty string).")]
    public async Task<CallToolResult> ClearAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent adact_snapshot.")]
        string @ref,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var error)) return error!;
        try
        {
            await session!.ClearAsync(@ref, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_clear"); }
    }

    /// <summary>ScrollItemPattern で要素を表示領域内へスクロールする。</summary>
    /// <param name="ref">対象 element ref。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。Pattern 不在は <c>ELEMENT_INTERACTION_FAILED</c>。</returns>
    [McpServerTool(Name = "adact_scroll")]
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
        catch (Exception ex) { return MapOrLog(ex, "adact_scroll"); }
    }
}
