# `adact snapshot`

Capture a fresh UIA snapshot of the active or specified session. The snapshot
contains the UIA element tree and is the canonical source of element refs
(`s<sid>e<eid>`) for `click` and `fill`.

`attach`, `click` and `fill` already capture snapshots automatically, so you
only need to call `snapshot` explicitly when:

- the UI changed asynchronously (timer, background task, OS dialog), and
- you want a refreshed view without performing another action.

## Synopsis

```
adact snapshot [--sid <sessionId>] [--snapshot-dir <dir>] [--server <url>]
```

| Flag              | Purpose                                                                  |
| ----------------- | ------------------------------------------------------------------------ |
| `--sid`           | Target session id (e.g. `s1`). Defaults to the active session.           |
| `--snapshot-dir`  | Output directory (default `./.adact/`).                                  |
| `--server`        | Daemon endpoint URL.                                                     |

## Output

```
sessionId s1
snapshot .adact/session-1-20260428T180001234.json
```

Filenames are `session-<sid>-<UTC timestamp>.json`. Older files are not
cleaned up automatically — older snapshots in the same directory remain valid
historical artifacts but their refs may be stale.

## Element refs are stable across snapshots

Within a session, the same UIA element keeps the same `eid` across successive
snapshots whenever ADACT can identify it (primarily via `RuntimeId`). That
means a ref taken from snapshot N usually still works in snapshot N+1, so you
can keep clicking through a workflow without re-deriving refs every time.

When an element is replaced or destroyed (a dialog closes, a virtualized list
item scrolls out of view, etc.) its ref becomes `REF_NOT_FOUND`; recover by
re-capturing a snapshot and finding the element by role/name/AutomationId.

## Examples

Snapshot the active session:

```
adact snapshot
```

Snapshot a specific session, writing the file to a custom directory:

```
adact snapshot --sid s1 --snapshot-dir ./out/snapshots
```

## Error recovery

- `NO_ACTIVE_SESSION` — no session is attached and `--sid` was omitted. Run
  `adact attach` first, or pass `--sid` explicitly.
- `INVALID_ARGUMENT` — `--sid` referred to a session that no longer exists
  (the window was closed) or was never created. Re-attach.
- `SNAPSHOT_FAILED` — UIA could not capture the tree. The window may be
  closing or unresponsive; retry once, then re-attach if it persists.
