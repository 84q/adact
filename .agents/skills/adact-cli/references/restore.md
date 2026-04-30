# `adact restore`

Restore a minimized or maximized window to its normal state via UIA
`WindowPattern.SetWindowVisualState(Normal)`. After a successful restore
ADACT automatically captures a fresh snapshot.

## Synopsis

```
adact restore [--sid <sid>] [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

| Argument / Flag   | Purpose                                                                    |
| ----------------- | -------------------------------------------------------------------------- |
| `--sid`           | Target session ID (e.g. `s1`). Defaults to the active session.             |
| `--no-snapshot`   | Skip the automatic post-restore snapshot.                                  |
| `--snapshot-dir`  | Output directory for the post-restore snapshot (default `./.adact/`).      |
| `--server`        | Daemon endpoint URL.                                                       |

## Output

```
sessionId s1
snapshot .adact/session-1-20260428T180006300.txt
```

With `--no-snapshot`, only `sessionId` is printed.

## Examples

Restore the active window (e.g. after `minimize`):

```
adact restore
```

## Error recovery

- `NO_ACTIVE_SESSION` — no session is attached. Run `adact attach` first or
  pass `--sid`.
- `NOT_FOUND` — the specified `--sid` does not match any live session.
- `ELEMENT_INTERACTION_FAILED` — the window does not expose `WindowPattern`.
  No CLI-side recovery is possible.
