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
adact snapshot [--sid <sessionId>] [--snapshot-dir <dir>] [--filter <name>] [--server <url>]
```

| Flag              | Purpose                                                                  |
| ----------------- | ------------------------------------------------------------------------ |
| `--sid`           | Target session id (e.g. `s1`). Defaults to the active session.           |
| `--snapshot-dir`  | Output directory (default `./.adact/`).                                  |
| `--filter`        | Tree filter: `operable` (default, AI-friendly) or `raw` (full UIA tree). |
| `--server`        | Daemon endpoint URL.                                                     |

## Output

```
sessionId s1
snapshot .adact/session-1-20260428T180001234.txt
---
filter: operable
sessionId: s1
...
---
- Window "メモ帳" [ref=s1e1]
  ...
```

The snapshot content is written to **both** stdout and the file. Filenames are
`session-<sid>-<UTC timestamp>.txt`. The file content is a Playwright-style
indented text representation of the UIA tree (UTF-8, LF newlines, no BOM).
Older files are not cleaned up automatically.

If the UI state has not changed since the last snapshot, ADACT reuses the
existing file and prints `unchanged true`:

```
sessionId s1
snapshot .adact/session-1-20260428T180001234.txt
unchanged true
---
filter: operable
...
```

### File format

```
---
filter: operable
sessionId: s1
processName: notepad
processId: 1234
generatedAt: "2025-01-01T00:00:00Z"
---
- Window "メモ帳" [ref=s1e1]
  - MenuBar [ref=s1e2]
    - MenuItem "ファイル" [ref=s1e3]
  - Edit [aid="15.Edit"] [value="hello"] [focused] [ref=s1e7]
```

Frontmatter values are bare YAML scalars when they contain only alphanumerics,
spaces, `_`, or `-`; otherwise they are wrapped in double quotes (e.g.
ISO-8601 timestamps and Japanese strings).

Each body line follows:

```
<indent>- <ControlType> ["<Name>"] [aid="<id>"] [value="<v>"] [<state-flags>] [ref=<refId>]
```

Attribute order is fixed: **aid → value → state-flags (`disabled` /
`focused` / `modal`) → ref**. Indent is 2 spaces per depth. Strings (`name`,
`aid`, `value`) are quoted; `"`, `\`, LF, TAB are escaped (`\"`, `\\`, `\n`,
`\t`); other control characters become `\uXXXX`; Unicode (e.g. Japanese) is
emitted verbatim.

### Filters

- `operable` (default): AI-friendly subset. Keeps interactive controls
  (`Window`, `Button`, `MenuItem`, `Edit`, `CheckBox`, `Tab`, `ListItem`, …).
  Structural containers (`Pane`, `Group`, `Custom`, `Image`, `Separator`) are
  flattened unless they have a `Name` or `AutomationId`. Off-screen elements
  are excluded together with their descendants.
- `raw`: includes every UIA element, including off-screen and structural
  nodes. Useful when an element you need is being filtered out by `operable`.

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

Get the full UIA tree (including structural / off-screen elements):

```
adact snapshot --filter raw
```

## Error recovery

- `NO_ACTIVE_SESSION` — no session is attached and `--sid` was omitted. Run
  `adact attach` first, or pass `--sid` explicitly.
- `INVALID_ARGUMENT` — `--sid` referred to a session that no longer exists
  (the window was closed) or was never created, or `--filter` was not
  `operable` / `raw`.
- `SNAPSHOT_FAILED` — UIA could not capture the tree. The window may be
  closing or unresponsive; retry once, then re-attach if it persists.
