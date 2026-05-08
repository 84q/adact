using System.ComponentModel;

using Adact.Engine;
using Adact.Engine.Snapshot;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
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

    [McpServerTool(Name = "adact_select")]
    [Description("Select items in a list/combobox by Name ('name'), 0-based 'index', or child 'itemRef'. Provide one or more of a single kind. Use 'add' to keep existing selection, 'remove' to deselect.")]
    public async Task<CallToolResult> SelectAsync(
        [Description("Ref ID of the container (List, ComboBox, etc.).")]
        string @ref,
        [Description("Name(s) of the child item(s) to select.")]
        string[]? name = null,
        [Description("0-based index(es) of the child item(s) to select.")]
        int[]? index = null,
        [Description("Element ref(s) of the child ListItem(s) to select (from a recent snapshot).")]
        string[]? itemRef = null,
        [Description("When true, add to existing selection instead of replacing it.")]
        bool add = false,
        [Description("When true, remove from existing selection.")]
        bool remove = false,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var error)) return error!;

        if (add && remove)
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "'add' and 'remove' cannot both be true.");

        int kindCount = (name is { Length: > 0 } ? 1 : 0) + (index is { Length: > 0 } ? 1 : 0) + (itemRef is { Length: > 0 } ? 1 : 0);
        if (kindCount == 0)
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "select requires at least one of 'name', 'index', or 'itemRef'.");
        if (kindCount > 1)
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "Only one kind of selector ('name', 'index', or 'itemRef') may be specified.");

        if (itemRef is { Length: > 0 })
        {
            foreach (var ir in itemRef)
            {
                if (!RefId.TryParse(ir, out _, out _))
                    return ToolErrors.Error(ToolErrors.InvalidRefFormat, $"Ref ID '{ir}' is malformed.");
            }
        }

        SelectionTarget[] targets;
        if (name is { Length: > 0 })
            targets = name.Select(SelectionTarget.FromName).ToArray();
        else if (index is { Length: > 0 })
            targets = index.Select(SelectionTarget.FromIndex).ToArray();
        else
            targets = itemRef!.Select(SelectionTarget.FromItemRef).ToArray();

        var mode = add ? SelectionMode.Add : remove ? SelectionMode.Remove : SelectionMode.Replace;

        try
        {
            await session!.SelectAsync(@ref, targets, mode, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_select"); }
    }

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
