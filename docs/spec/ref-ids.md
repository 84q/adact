# Ref IDs

ADACT refers to Windows UIA elements and windows with short ref IDs. Refs are temporary IDs inside the daemon process; they are not persistent selectors.

## ID formats

| Kind | Format | Example | Owner | Purpose |
| --- | --- | --- | --- | --- |
| Window ref | `w<n>` | `w1` | `WindowRefStore` | Reference a top-level window for attach |
| Session id | `s<n>` | `s1` | `SessionStore` | Reference an attached window session |
| Element ref | `s<sid>e<eid>` | `s1e7` | `WindowSession` / `RefRegistry` | Reference a UIA element inside a snapshot |

Older discussion notes may still mention generation-based refs (`s<sid>g<gen>e<eid>`). The current implementation has removed generation, and new snapshots use `s<sid>e<eid>`.

## Window ref (`w<n>`)

| Item | Description |
| --- | --- |
| Issued when | `list-windows` runs |
| Stability | Reused for the same `WindowKey` |
| Invalidation | Becomes retired when the window disappears from the list |
| Main use | Picking a window from a list when the title or process name is ambiguous |

`WindowKey` uses HWND, process id, and process start time so the same window can be recognized even if the title changes.

## Session id (`s<n>`)

| Item | Description |
| --- | --- |
| Issued when | Attach succeeds |
| Stored in | `SessionStore` |
| Active session | The most recently attached session becomes active |
| Invalidation | `detach`, `close-window`, `kill`, `daemon-stop`, or daemon exit |
| Reuse | Never reused inside the same daemon process |

`snapshot`, `detach`, `close-window`, and `kill` use the active session when `sessionId` is omitted.

## Element ref (`s<sid>e<eid>`)

| Item | Description |
| --- | --- |
| Issued when | `WindowSession.SnapshotAsync()` walks the UIA tree |
| Prefix | `s<sid>` identifies the session |
| Stability | Elements with a RuntimeId reuse the same `eid` across snapshots |
| Fallback | When no RuntimeId exists, positional order is used |
| Invalidation | Session deletion, daemon exit, or the element missing from the current snapshot |

Element refs are temporary IDs used to interact with elements confirmed in the latest snapshot.

## Legacy generation-based format

| Format | Current handling |
| --- | --- |
| `s<sid>g<gen>e<eid>` | Legacy format kept in older notes and baselines |
| `generation` field | Removed from current MCP/CLI output |
| `gen-N` in snapshot file names | Removed from current output |

## Lifecycle

| Operation | `windowRef` | `sessionId` | `elementRef` |
| --- | --- | --- | --- |
| `list-windows` | Issued and synchronized | May appear if an existing session exists | Unchanged |
| `attach` | Associated with a session | Issued or reused | Issued on snapshot |
| `snapshot` | Unchanged | Preserved | Current snapshot element set updated |
| `click` / `fill` | Unchanged | Preserved | Updated by the post-action snapshot |
| `wait-for-element` | Unchanged | Preserved | Unchanged |
| `wait-for-window` | Unchanged | Unchanged | Unchanged |
| `launch` | Unchanged | Unchanged | N/A until later attach |
| `detach` | Association removed | Deleted | Invalidated |
| `close-window` / `kill` | Association removed | Deleted | Invalidated |
| `daemon-stop` | All refs disappear when the daemon exits | All refs disappear when the daemon exits | All refs disappear when the daemon exits |

## Invalidation summary

| Situation | Typical error | Recovery |
| --- | --- | --- |
| `w<n>` is unknown or retired | `INVALID_WINDOW_REF` | Run `list-windows` again |
| `s<n>` does not exist | `INVALID_ARGUMENT` or `NO_ACTIVE_SESSION` | Re-attach |
| `s<sid>e<eid>` is malformed | `INVALID_REF_FORMAT` or `REF_NOT_FOUND` | Copy the ref from the latest snapshot |
| Element is missing from the current snapshot | `REF_NOT_FOUND` | Re-snapshot and choose a new ref |
| Daemon restarted | Connection/state reset | Start from `list-windows` again |

## Stability policy

Element refs are intended to behave like Playwright MCP `_ariaRef` values: the same element should keep a short ref when possible. Current stability order is:

| Priority | Stable key | Notes |
| ---: | --- | --- |
| 1 | UIA RuntimeId | Stable enough for the primary supported apps |
| 2 | Positional fallback | Minimal guarantee when RuntimeId is unavailable |

## References

| Document | Description |
| --- | --- |
| [snapshot.md](snapshot.md) | Ref display format inside snapshots |
| [mcp-tools.md](mcp-tools.md) | Ref arguments for MCP tools |
