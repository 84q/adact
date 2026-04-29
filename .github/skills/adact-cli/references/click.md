# `adact click`

Click an element identified by an element ref. After a successful click ADACT
automatically captures a fresh snapshot so you can immediately decide on the
next step.

## Synopsis

```
adact click <ref> [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

| Argument / Flag   | Purpose                                                                    |
| ----------------- | -------------------------------------------------------------------------- |
| `<ref>`           | Element ref in `s<sid>e<eid>` form, taken from a snapshot.                 |
| `--no-snapshot`   | Skip the automatic post-click snapshot.                                    |
| `--snapshot-dir`  | Output directory for the post-click snapshot (default `./.adact/`).        |
| `--server`        | Daemon endpoint URL.                                                       |

## Output

```
sessionId s1
snapshot .adact/session-1-20260428T180002500.txt
```

With `--no-snapshot`, only `sessionId` is printed.

## Examples

Click button `s1e7`:

```
adact click s1e7
```

Click without taking a follow-up snapshot (useful in long batches when you
plan to call `snapshot` only at the end):

```
adact click s1e7 --no-snapshot
```

## Error recovery

- `INVALID_REF_FORMAT` — the ref is not `s<sid>e<eid>`. Copy the ref verbatim
  from the latest snapshot.
- `REF_NOT_FOUND` — the element no longer exists (dialog closed, virtualized
  list scrolled, control replaced). Recover by:
  1. Run `adact snapshot` to get a fresh tree.
  2. Locate the target element again by its role / name / AutomationId.
  3. Retry with the new ref.
- `ELEMENT_INTERACTION_FAILED` — the element exists but cannot be invoked
  (offscreen, disabled, or covered by another window). Make sure the window
  is in the foreground, scroll the control into view if needed, then retry.
- `NO_ACTIVE_SESSION` — no session is attached. Run `adact attach` first.
