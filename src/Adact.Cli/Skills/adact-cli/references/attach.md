# `adact attach`

Attach to a top-level window and start (or reuse) an ADACT session. On
success, ADACT also captures a snapshot of the window so you can immediately
read the UIA tree.

## Synopsis

```
adact attach <windowRef>
adact attach --process-name <name> [--title <title>]
                                   [--process-id <pid>]
                                   [--class-name <class>]
             [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

You must supply **either** the positional `windowRef` (from `list-apps`) **or**
at least one of the matching flags. The two forms are mutually exclusive.

| Flag              | Purpose                                                                     |
| ----------------- | --------------------------------------------------------------------------- |
| `--process-name`  | Process name (case-insensitive, exact match), e.g. `Notepad`.               |
| `--title`         | Window title (case-insensitive, exact match).                               |
| `--process-id`    | Process id (integer).                                                       |
| `--class-name`    | Win32 class name.                                                           |
| `--no-snapshot`   | Skip the automatic post-attach snapshot.                                    |
| `--snapshot-dir`  | Output directory for the snapshot (default `./.adact/`).                    |
| `--server`        | Daemon endpoint URL.                                                        |

All flags must combine to match **exactly one** window.

## Output

```
sessionId s1
windowRef w1
snapshot .adact/session-1-20260428T180000000.json
```

- `sessionId` is needed for follow-up calls to `snapshot` (`--sid`).
- `windowRef` lets you re-attach later without rediscovering the window.
- `snapshot` is the path to the JSON file containing the UIA tree.

With `--no-snapshot`, only the first two lines are printed.

## Examples

Attach by window ref returned from `list-apps`:

```
adact attach w1
```

Attach by process name when only one instance is running:

```
adact attach --process-name Notepad
```

Disambiguate two Notepad windows by adding the title:

```
adact attach --process-name Notepad --title "Untitled - Notepad"
```

Attach without snapshot (e.g. when you intend to call `snapshot` yourself
with custom options):

```
adact attach w1 --no-snapshot
```

## Error recovery

- `INVALID_ARGUMENT` — raised by the CLI before contacting the daemon. Two
  cases:
  - Both positional ref and matching flags were supplied, or none of them
    were. Pick exactly one form.
  - The positional ref does not match the `w<n>` format (e.g. `attach foo`
    or `attach w`). Use a `windowRef` printed by `list-apps` verbatim.
- `INVALID_WINDOW_REF` — raised by the daemon. The ref is well-formed
  (`w<n>`) but the daemon does not know it: either it was never issued, or
  the underlying window has been closed and the ref retired. Re-run
  `adact list-apps` and use the freshly printed `windowRef`.
- `AMBIGUOUS_ATTACH` — more than one window matched. Add more flags
  (typically `--process-id` taken from `list-apps`) until only one window
  matches.
- `WINDOW_NOT_FOUND` — no window matched the supplied flags. Confirm the
  window is open with `list-apps` and relax/correct the criteria.
- `CONNECTION_FAILED` — see [`SKILL.md`](../SKILL.md) "Connecting to the
  daemon".
