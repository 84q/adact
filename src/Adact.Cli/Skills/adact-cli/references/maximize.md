# `adact maximize`

Maximize the attached window via UIA
`WindowPattern.SetWindowVisualState(Maximized)`. After a successful maximize
ADACT automatically captures a fresh snapshot.

## Synopsis

```
adact maximize [--sid <sid>] [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

| Argument / Flag   | Purpose                                                                    |
| ----------------- | -------------------------------------------------------------------------- |
| `--sid`           | Target session ID (e.g. `s1`). Defaults to the active session.             |
| `--no-snapshot`   | Skip the automatic post-maximize snapshot.                                 |
| `--snapshot-dir`  | Output directory for the post-maximize snapshot (default `./.adact/`).     |
| `--server`        | Daemon endpoint URL.                                                       |

## Output

```
sessionId s1
snapshot .adact/session-1-20260428T180005800.txt
```

With `--no-snapshot`, only `sessionId` is printed.

## Examples

Maximize the active window:

```
adact maximize
```

## Error recovery

- `NO_ACTIVE_SESSION` — no session is attached. Run `adact attach` first or
  pass `--sid`.
- `NOT_FOUND` — the specified `--sid` does not match any live session.
- `ELEMENT_INTERACTION_FAILED` — the window does not expose `WindowPattern`,
  or its `CanMaximize` is false (typical for fixed-size dialogs and pop-ups).
  No CLI-side recovery is possible.
