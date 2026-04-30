# `adact attach`

Attach to a top-level window and start (or reuse) an ADACT session. On
success, ADACT also captures a snapshot of the window so you can immediately
read the UIA tree.

## Synopsis

```
adact attach <windowRef> [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

`windowRef` is the `w<n>` id printed by `adact list-apps`. It is the only
way to specify the target window — there are no `--process-name`, `--title`,
`--process-id`, or `--class-name` matching flags. Always run `list-apps`
first and pass the resulting `w<n>` verbatim.

| Flag              | Purpose                                                                     |
| ----------------- | --------------------------------------------------------------------------- |
| `--no-snapshot`   | Skip the automatic post-attach snapshot.                                    |
| `--snapshot-dir`  | Output directory for the snapshot (default `./.adact/`).                    |
| `--server`        | Daemon endpoint URL.                                                        |

## Output

```
sessionId s1
windowRef w1
snapshot .adact/session-1-20260428T180000000.txt
```

- `sessionId` is needed for follow-up calls to `snapshot` (`--sid`).
- `windowRef` echoes the input (or the canonical ref of an idempotently
  reused session for the same window).
- `snapshot` is the path to the Playwright-style snapshot text file
  (`.txt`) containing the UIA tree. See [`snapshot.md`](snapshot.md) for the
  format details.

With `--no-snapshot`, only the first two lines are printed.

## Examples

Attach by window ref returned from `list-apps`:

```
adact list-apps
adact attach w1
```

Attach without snapshot (e.g. when you intend to call `snapshot` yourself
with custom options):

```
adact attach w1 --no-snapshot
```

## Error recovery

- `INVALID_ARGUMENT` — raised by the CLI before contacting the daemon. The
  positional ref is missing or does not match the `w<n>` format (e.g.
  `attach foo`). Use a `windowRef` printed by `list-apps` verbatim.
- `INVALID_WINDOW_REF` — raised by the daemon. The ref is well-formed
  (`w<n>`) but the daemon does not know it: either it was never issued, or
  the underlying window has been closed and the ref retired. Re-run
  `adact list-apps` and use the freshly printed `windowRef`.
- `WINDOW_NOT_FOUND` — the daemon resolved the ref but failed to attach to
  the underlying HWND (the window may have just been closed). Re-run
  `list-apps` and confirm the target window is still present.
- `CONNECTION_FAILED` — see [`SKILL.md`](../SKILL.md) "Connecting to the
  daemon".
