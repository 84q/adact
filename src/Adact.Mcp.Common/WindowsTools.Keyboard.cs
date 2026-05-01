using System.ComponentModel;

using Adact.Engine;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Adact.Mcp.Common;

public sealed partial class WindowsTools
{
    /// <summary>キーコンボ ("Ctrl+Shift+E" 等) を 1 回 press する。ref 指定時はそれにフォーカスしてから送出。</summary>
    /// <param name="key">キー記述。"Enter", "F5", "Ctrl+C" など。</param>
    /// <param name="ref">フォーカス対象 (任意)。null の場合は active session のウィンドウ。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_press")]
    [Description("Press a key combo such as 'Ctrl+C' or 'Enter'. If 'ref' is provided, that element is focused first; otherwise the active session's window is used.")]
    public async Task<CallToolResult> PressAsync(
        [Description("Key combo (e.g. 'Enter', 'F5', 'Ctrl+Shift+E').")]
        string key,
        [Description("Optional element ref to focus before pressing. Omit to use the active session's window.")]
        string? @ref = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrEmpty(key))
            return ToolErrors.Error(ToolErrors.InvalidArgument, "key must be a non-empty string.");

        IWindowSession? session;
        if (@ref is null)
        {
            session = _store.GetActiveOrNull();
            if (session is null)
                return ToolErrors.Error(ToolErrors.NoActiveSession,
                    "No active session: attach to a window first or specify 'ref'.");
        }
        else
        {
            if (!ValidateRef(@ref, out session, out var refError)) return refError!;
        }

        try
        {
            await session!.PressAsync(key, @ref, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (ArgumentException ex)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, ex.Message);
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_press"); }
    }

    /// <summary>単一キーを押し下げる (release は <see cref="KeyUpAsync"/>)。active session のウィンドウに送る。</summary>
    /// <param name="key">単一キー名。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_key_down")]
    [Description("Press and hold a single key on the active session's window. Pair with windows_key_up to release.")]
    public async Task<CallToolResult> KeyDownAsync(
        [Description("Single key name (e.g. 'Shift', 'A', 'F1'). Combinations with '+' are not allowed here.")]
        string key,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(key))
            return ToolErrors.Error(ToolErrors.InvalidArgument, "key must be a non-empty string.");

        var session = _store.GetActiveOrNull();
        if (session is null)
            return ToolErrors.Error(ToolErrors.NoActiveSession,
                "No active session: attach to a window first.");

        try
        {
            await session.KeyDownAsync(key, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (ArgumentException ex)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, ex.Message);
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_key_down"); }
    }

    /// <summary>単一キーを解放する (<see cref="KeyDownAsync"/> と対で使用)。</summary>
    /// <param name="key">単一キー名。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_key_up")]
    [Description("Release a single key previously pressed by windows_key_down on the active session's window.")]
    public async Task<CallToolResult> KeyUpAsync(
        [Description("Single key name (must match the one passed to windows_key_down).")]
        string key,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(key))
            return ToolErrors.Error(ToolErrors.InvalidArgument, "key must be a non-empty string.");

        var session = _store.GetActiveOrNull();
        if (session is null)
            return ToolErrors.Error(ToolErrors.NoActiveSession,
                "No active session: attach to a window first.");

        try
        {
            await session.KeyUpAsync(key, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (ArgumentException ex)
        {
            return ToolErrors.Error(ToolErrors.InvalidArgument, ex.Message);
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_key_up"); }
    }

    /// <summary>指定要素にフォーカスし、テキストを (オプションで遅延しながら) 1 文字ずつ Type する。</summary>
    /// <param name="ref">入力対象 element ref。</param>
    /// <param name="text">入力するテキスト。</param>
    /// <param name="delayMs">各文字間に挟む遅延 (ms)。0 以下で遅延なし。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>成功時は空 content。</returns>
    [McpServerTool(Name = "windows_type")]
    [Description("Focus the element and type the given text character by character. Use windows_fill for atomic value-pattern set.")]
    public async Task<CallToolResult> TypeAsync(
        [Description("Ref ID in the form 's<sid>e<eid>' obtained from a recent windows_snapshot.")]
        string @ref,
        [Description("Text to type.")]
        string text,
        [Description("Delay between characters in milliseconds. 0 (default) means no delay.")]
        int? delayMs = null,
        CancellationToken ct = default)
    {
        using var _lock = await _store.AcquireAsync(ct).ConfigureAwait(false);
        if (!ValidateRef(@ref, out var session, out var refError)) return refError!;
        if (text is null)
            return ToolErrors.Error(ToolErrors.InvalidArgument, "text must not be null.");
        if (delayMs is { } d && d < 0)
            return ToolErrors.Error(ToolErrors.InvalidArgument, "delayMs must be >= 0.");

        try
        {
            await session!.TypeAsync(@ref, text, delayMs ?? 0, ct).ConfigureAwait(false);
            return new CallToolResult { Content = [] };
        }
        catch (Exception ex) { return MapOrLog(ex, "windows_type"); }
    }
}
