# `adact minimize`

Minimize the attached window via UIA
`WindowPattern.SetWindowVisualState(Minimized)`. After a successful minimize
ADACT automatically captures a fresh snapshot.

Minimized windows have no on-screen coordinates, so element interactions that
depend on bounding rectangles (e.g. `mouse-move x,y`) may fail until the
window is restored. Coordinate-free UIA actions (Invoke, Toggle, SetValue,
SelectionItem.Select) generally still work while minimized.

## Synopsis

```
adact minimize [--sid <sid>] [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

| Argument / Flag   | Purpose                                                                    |
| ----------------- | -------------------------------------------------------------------------- |
| `--sid`           | Target session ID (e.g. `s1`). Defaults to the active session.             |
| `--no-snapshot`   | Skip the automatic post-minimize snapshot.                                 |
| `--snapshot-dir`  | Output directory for the post-minimize snapshot (default `./.adact/`).     |
| `--server`        | Daemon endpoint URL.                                                       |

## Output

```
sessionId s1
snapshot .adact/session-1-20260428T180005100.txt
```

With `--no-snapshot`, only `sessionId` is printed.

## Examples

Minimize the active window:

```
adact minimize
```

Minimize without re-snapshotting (recommended when you intend to keep the
window minimized for a while):

```
adact minimize --no-snapshot
```

## Error recovery

- `NO_ACTIVE_SESSION` — no session is attached. Run `adact attach` first or
  pass `--sid`.
- `NOT_FOUND` — the specified `--sid` does not match any live session.
- `ELEMENT_INTERACTION_FAILED` — the window does not expose `WindowPattern`.
  Most top-level windows support it; tooltips and pop-ups may not.
