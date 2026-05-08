# MCP Tools Specification

ADACT MCP tools are implemented in `src/Adact.Mcp.Common/WindowsTools.cs` and the `WindowsTools.{Mouse,Keyboard,Toggle,Window,Inspect,Wait,Launch,Screenshot}.cs` partial classes. They are used by `adact serve http` and `adact serve pipe`, and can also be called directly by MCP clients.

The CLI does not print these MCP responses as-is; it reformats them into YAML-style, TSV-style, or snapshot output.

## Tool list

| Category | Tool | Role | Main arguments | Main return |
| --- | --- | --- | --- | --- |
| Discovery | `adact_list_windows` | Enumerate top-level windows on the current desktop | none | `windows[]` with `windowRef`, optional `sessionId`, `processName`, `processId`, optional `className`, and `windowTitle` |
| Session | `adact_attach` | Attach to one window | `windowRef` (required) | `sessionId`, `windowRef`, `windowInfo` |
| Session | `adact_snapshot` | Return the raw UIA snapshot for the attached window | optional `sessionId` | Raw JSON (`_meta`, `tree`) |
| Mouse | `adact_click` | Click an element by ref | `ref`, optional button/count/modifiers/position | Empty content on success |
| Mouse | `adact_doubleclick` | Double-click an element by ref | `ref`, optional button/modifiers/position | Empty content on success |
| Mouse | `adact_hover` | Hover an element by ref | `ref`, optional modifiers/position | Empty content on success |
| Mouse | `adact_mousemove` | Move the cursor to coordinates | `target` | Empty content on success |
| Mouse | `adact_mousedown` / `adact_mouseup` | Press or release at the current cursor position | optional `button` | Empty content on success |
| Mouse | `adact_mousewheel` | Scroll at the current cursor position | optional `deltaX`, `deltaY` | Empty content on success |
| Keyboard | `adact_fill` | Fill an element by ref | `ref`, `value` | Empty content on success |
| Keyboard | `adact_type` | Type text into an element by ref | `ref`, `text`, optional `delayMs` | Empty content on success |
| Keyboard | `adact_keypress` | Send a key combination | `key` | Empty content on success |
| Keyboard | `adact_keydown` / `adact_keyup` | Press or release a key | `key` | Empty content on success |
| Toggle | `adact_check` / `adact_uncheck` | Toggle a checkable element | `ref` | Empty content on success |
| Toggle | `adact_select` | Select a list or combo-box item | `ref`, and one of `name`, `index`, or `itemRef` | Empty content on success |
| Toggle | `adact_focus` | Move keyboard focus | `ref` | Empty content on success |
| Toggle | `adact_scroll_into_view` | Scroll an item into view | `ref` | Empty content on success |
| Toggle | `adact_scroll` | Scroll a container | `ref`, optional scroll values | Empty content on success |
| Window | `adact_resize_window` | Resize the attached window | optional `width`, `height`, `sessionId` | Empty content on success |
| Window | `adact_minimize_window` / `adact_maximize_window` / `adact_restore_window` | Change the attached window state | optional `sessionId` | Empty content on success |
| Inspect | `adact_inspect` | Return detailed UIA properties | `ref` | Inspect JSON |
| Inspect | `adact_screenshot` | Save a PNG | optional `ref`, `out`, `sessionId` | `{ sessionId, path, width, height }` |
| Wait | `adact_wait_for_element` | Wait for an element state | `ref` or query fields, optional `state`, `timeoutMs`, `sessionId` | `{ sessionId, ref, state }` |
| Wait | `adact_wait_for_window` | Wait for a top-level window without attaching | optional `title`, `className`, `processName`, `executable`, `timeoutMs` | Window info JSON |
| Lifecycle | `adact_launch` | Start a Win32/.NET/UWP process without attaching | `executable`, optional `args`, `cwd`, `env` | `{ pid, processName, executablePath }` |
| Lifecycle | `adact_detach` | Release a session record | optional `sessionId` | `sessionId`, `detached: true` |
| Lifecycle | `adact_close_window` | Close the attached window and release the session | optional `sessionId` | `sessionId`, `closed: true`, `detached: true` |
| Lifecycle | `adact_kill` | Force-kill the attached process and release the session | optional `force`, `timeoutMs`, `sessionId` | `sessionId`, `killed: true`, `detached: true`, `method` |
| Lifecycle | `adact_daemon_stop` | Stop the current daemon | none | `stopped: true` |

## Tool details

The remaining sections from the original design are preserved at a high level: each tool resolves the session or window it targets, performs the Engine operation, and returns a structured success or error result. The CLI layer is responsible for the final user-facing formatting.

### Error handling

Business and input errors are returned as MCP tool results, not JSON-RPC errors.

| Field | Description |
| --- | --- |
| `isError` | `true` |
| text content | `<CODE>: <message>` |
| structured content | `{ "code": "...", "message": "...", "details": ... }` |

Transport/protocol/system errors are handled by the SDK as JSON-RPC errors.

## Representative error codes

| Code | Typical cause |
| --- | --- |
| `INVALID_ARGUMENT` | Missing argument, unknown session id, or invalid combination |
| `INVALID_REF_FORMAT` | Element ref is not in `s<sid>e<eid>` form |
| `INVALID_WINDOW_REF` | `w<n>` is unregistered or retired |
| `WINDOW_NOT_FOUND` | HWND attach failed after resolving `windowRef` |
| `REF_NOT_FOUND` | Element ref is malformed, belongs to another session, or is not in the current snapshot |
| `ELEMENT_INTERACTION_FAILED` | UIA click/fill or similar operation failed |
| `SNAPSHOT_FAILED` | Snapshot construction failed |
| `NO_ACTIVE_SESSION` | Session id was omitted and no active session exists |
| `NOT_FOUND` | The requested session does not exist |
| `CLOSE_FAILED` | Close failed |
| `KILL_FAILED` | Kill failed |
| `LAUNCH_FAILED` | Launch failed |
| `WAIT_TIMEOUT` | Wait timed out |
| `LOCAL_ONLY` | A local-only command was run against a remote target |
| `INTERNAL_ERROR` | Unexpected internal failure |

## References

| Document | Description |
| --- | --- |
| [cli.md](cli.md) | CLI usage |
| [ref-ids.md](ref-ids.md) | Ref and session formats |
| [errors-and-output.md](errors-and-output.md) | MCP error and CLI error mapping |
