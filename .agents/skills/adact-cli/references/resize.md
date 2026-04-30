# `adact resize`

Resize the attached window via UIA `TransformPattern.Resize`. After a
successful resize ADACT automatically captures a fresh snapshot.

## Synopsis

```
adact resize --width <w> --height <h>
             [--sid <sid>] [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

| Argument / Flag   | Purpose                                                                    |
| ----------------- | -------------------------------------------------------------------------- |
| `--width`         | New window width in pixels. Must be > 0.                                   |
| `--height`        | New window height in pixels. Must be > 0.                                  |
| `--sid`           | Target session ID (e.g. `s1`). Defaults to the active session.             |
| `--no-snapshot`   | Skip the automatic post-resize snapshot.                                   |
| `--snapshot-dir`  | Output directory for the post-resize snapshot (default `./.adact/`).       |
| `--server`        | Daemon endpoint URL.                                                       |

## Output

```
sessionId s1
snapshot .adact/session-1-20260428T180004500.txt
```

With `--no-snapshot`, only `sessionId` is printed.

## Examples

Resize the active window to 1024×768:

```
adact resize --width 1024 --height 768
```

Resize a specific session without taking a follow-up snapshot:

```
adact resize --width 800 --height 600 --sid s2 --no-snapshot
```

## Error recovery

- `INVALID_ARGUMENT` — `--width` or `--height` is missing or not positive.
  Pass both options as integers > 0.
- `NO_ACTIVE_SESSION` — no session is attached and `--sid` was not specified.
  Run `adact attach` first or pass `--sid` explicitly.
- `NOT_FOUND` — the specified `--sid` does not match any live session. Run
  `adact list-apps` and re-attach if needed.
- `ELEMENT_INTERACTION_FAILED` — the window does not support resize
  (`TransformPattern` unavailable or `CanResize = false`). This is expected
  for fixed-size dialogs and tool windows; no recovery is possible from the
  CLI side.
