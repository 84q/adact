using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Adact.Engine;
using Adact.Engine.Snapshot;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    /// <summary>Default timeout for <c>wait-for</c> / <c>wait-for-window</c> in milliseconds. See design 022 §6 / §13.</summary>
    private const int WaitForDefaultTimeoutMs = 5000;

    /// <summary>
    /// Waits for an element to reach a requested state (design 022 §6 / §7).
    /// Supports either a ref or search conditions. No auto-snapshot is captured.
    /// </summary>
    /// <param name="ref">Element ref to wait on (for example, <c>s1e7</c>). Mutually exclusive with search conditions.</param>
    /// <param name="name">Search condition: exact match on UIA Name (case-insensitive).</param>
    /// <param name="controlType">Search condition: exact match on UIA ControlType (case-insensitive).</param>
    /// <param name="automationId">Search condition: exact match on AutomationId (case-insensitive).</param>
    /// <param name="className">Search condition: exact match on ClassName (case-insensitive).</param>
    /// <param name="state">Target state: attached, detached, visible, hidden, enabled, or disabled. Defaults to <c>visible</c>.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Defaults to 5000.</param>
    /// <param name="sessionId">Target session. Null means the active session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>JSON <c>{ ref, state }</c>. Throws <c>WAIT_TIMEOUT</c> on timeout.</returns>
    [McpServerTool(Name = "adact_wait_for_element")]
    [Description("Wait until an element reaches a state. Pass either a ref (ref mode) OR search conditions (name/controlType/automationId/className) — they are mutually exclusive. State defaults to 'visible'. No snapshot is captured.")]
    public async Task<CallToolResult> WaitForAsync(
        [Description("Element ref like 's1e7' to wait on. Mutually exclusive with name/controlType/automationId/className.")]
        string? @ref = null,
        [Description("Search condition: UIA Name exact match (case-insensitive).")]
        string? name = null,
        [Description("Search condition: UIA ControlType exact match (case-insensitive, e.g. 'Button').")]
        string? controlType = null,
        [Description("Search condition: AutomationId exact match (case-insensitive).")]
        string? automationId = null,
        [Description("Search condition: ClassName exact match (case-insensitive).")]
        string? className = null,
        [Description("Target state: 'attached', 'detached', 'visible', 'hidden', 'enabled', 'disabled'. Defaults to 'visible'.")]
        string? state = null,
        [Description("Polling timeout in milliseconds. Defaults to 5000.")]
        int? timeoutMs = null,
        [Description("Session ID like 's1'. Omit for active session.")]
        string? sessionId = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        // Normalize the requested state.
        var stateStr = string.IsNullOrEmpty(state) ? "visible" : state;
        if (!WaitForStateParser.TryParse(stateStr, out var parsedState))
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                $"state '{stateStr}' is not one of: {WaitForStateParser.AllowedValues}.");
        }

        // Normalize the timeout.
        var timeoutMsValue = timeoutMs ?? WaitForDefaultTimeoutMs;
        if (timeoutMsValue <= 0)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, "timeoutMs must be > 0.");
        }
        var timeout = TimeSpan.FromMilliseconds(timeoutMsValue);

        // Either ref mode or search mode.
        var hasRef = !string.IsNullOrEmpty(@ref);
        var query = new WaitForElementQuery(name, controlType, automationId, className);
        var hasQuery = query.HasAnyCondition;
        if (hasRef && hasQuery)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "ref and search conditions (name/controlType/automationId/className) are mutually exclusive.");
        }
        if (!hasRef && !hasQuery)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "Specify either ref or at least one of name/controlType/automationId/className.");
        }
        // Detached state is not supported in query mode.
        if (hasQuery && parsedState == WaitForState.Detached)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "'detached' state is not supported in query mode.");
        }
        // Ref mode resolves the session from the ref, so sessionId must not be set.
        if (hasRef && !string.IsNullOrEmpty(sessionId))
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "sessionId must not be specified together with ref (the session is resolved from ref).");
        }

        // Resolve the target session.
        IWindowSession? session;
        if (hasRef)
        {
            if (!RefId.TryParse(@ref!, out _, out _))
            {
                return ToolErrors.Error(ToolErrors.RefNotFound, $"Ref ID '{@ref}' is malformed.");
            }
            session = _store.ResolveByRef(@ref!);
            if (session is null)
            {
                return ToolErrors.Error(ToolErrors.RefNotFound,
                    $"Ref ID '{@ref}' does not match any known session.");
            }
        }
        else
        {
            if (sessionId is null)
            {
                session = _store.GetActiveOrNull();
                if (session is null)
                {
                    return ToolErrors.Error(ToolErrors.NoActiveSession,
                        "No active session. Call adact_attach first or specify sessionId explicitly.");
                }
            }
            else if (!_store.TryGet(sessionId, out session))
            {
                return ToolErrors.Error(ToolErrors.NotFound, $"Session '{sessionId}' not found.");
            }
        }

        try
        {
            var result = hasRef
                ? await session!.WaitForRefAsync(@ref!, parsedState, timeout, ct).ConfigureAwait(false)
                : await session!.WaitForQueryAsync(query, parsedState, timeout, ct).ConfigureAwait(false);

            var json = new JsonObject
            {
                ["sessionId"] = $"s{session!.SessionId}",
                ["ref"] = result.Ref,
                ["state"] = WaitForStateParser.ToWireString(result.State),
            };
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = json.ToJsonString() }],
                StructuredContent = JsonSerializer.SerializeToElement(json),
            };
        }
        catch (ArgumentException ex)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, ex.Message);
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_wait_for_element"); }
    }

    /// <summary>
    /// Waits for a matching top-level window to appear (design 022 §6 / §7). Does not attach.
    /// </summary>
    /// <param name="title">Window title regex.</param>
    /// <param name="className">Win32 ClassName regex.</param>
    /// <param name="processName">Process name regex.</param>
    /// <param name="executable">Executable full-path regex.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Defaults to 5000.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Window info JSON. Throws <c>WAIT_TIMEOUT</c> on timeout.</returns>
    [McpServerTool(Name = "adact_wait_for_window")]
    [Description("Wait until a top-level window matching the given conditions appears. Does not attach. At least one of title/className/processName/executable must be specified. All fields are case-insensitive regex.")]
    public async Task<CallToolResult> WaitForWindowAsync(
        [Description("Window title regex (case-insensitive).")]
        string? title = null,
        [Description("Win32 ClassName regex (case-insensitive).")]
        string? className = null,
        [Description("Process name regex (case-insensitive, no extension).")]
        string? processName = null,
        [Description("Executable full-path regex (case-insensitive).")]
        string? executable = null,
        [Description("Polling timeout in milliseconds. Defaults to 5000.")]
        int? timeoutMs = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        var query = new WindowSearchQuery(title, className, processName, executable);
        if (!query.HasAnyCondition)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument,
                "Specify at least one of title/className/processName/executable.");
        }

        var timeoutMsValue = timeoutMs ?? WaitForDefaultTimeoutMs;
        if (timeoutMsValue <= 0)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, "timeoutMs must be > 0.");
        }
        var timeout = TimeSpan.FromMilliseconds(timeoutMsValue);

        try
        {
            var info = await _store.Engine.WaitForWindowAsync(query, timeout, ct).ConfigureAwait(false);

            var json = new JsonObject
            {
                ["processId"] = info.ProcessId,
                ["processName"] = info.ProcessName,
                ["windowTitle"] = info.Title,
                ["controlType"] = info.ControlType,
                ["className"] = info.ClassName,
                ["nativeWindowHandle"] = info.NativeWindowHandle.ToInt64(),
            };
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = json.ToJsonString() }],
                StructuredContent = JsonSerializer.SerializeToElement(json),
            };
        }
        catch (ArgumentException ex)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, ex.Message);
        }
        catch (Exception ex) { return MapOrLog(ex, "adact_wait_for_window"); }
    }
}
