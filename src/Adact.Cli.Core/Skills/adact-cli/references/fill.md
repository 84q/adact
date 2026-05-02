# `adact fill`

Overwrite the value of an input element (textbox, edit control) with the given
text. After a successful fill ADACT automatically captures a fresh snapshot.

`fill` replaces the entire current value; it does not append. It also does not
press Enter or any other key — use `click` on the appropriate button if the
target form needs to be submitted.

## Synopsis

```
adact fill <ref> <text> [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

| Argument / Flag   | Purpose                                                                    |
| ----------------- | -------------------------------------------------------------------------- |
| `<ref>`           | Element ref in `s<sid>e<eid>` form, taken from a snapshot.                 |
| `<text>`          | Text to write. Quote it on the shell if it contains spaces.                |
| `--no-snapshot`   | Skip the automatic post-fill snapshot.                                     |
| `--snapshot-dir`  | Output directory for the post-fill snapshot (default `./.adact/`).         |
| `--server`        | Daemon endpoint URL.                                                       |

## Output

```
sessionId s1
snapshot .adact/session-1-20260428T180003700.txt
```

With `--no-snapshot`, only `sessionId` is printed.

## Examples

Fill an address bar:

```
adact fill s1e12 "https://example.com"
```

Clear an input by passing an empty string:

```
adact fill s1e12 ""
```

## Error recovery

- `INVALID_REF_FORMAT` — the ref is not `s<sid>e<eid>`. Copy the ref verbatim
  from the latest snapshot.
- `INVALID_ARGUMENT` — `<text>` is missing. `fill` always takes two positional
  arguments; pass `""` for an empty value.
- `REF_NOT_FOUND` — the input was destroyed or replaced. Re-snapshot, locate
  the control again, retry with the new ref.
- `ELEMENT_INTERACTION_FAILED` — the element is not a text input or its value
  pattern is not available. Verify the ref points to an editable control (its
  `ControlType` is `Edit` or it exposes `ValuePattern`/`TextPattern`); if not,
  find the correct child element in the snapshot.
- `NO_ACTIVE_SESSION` — no session is attached. Run `adact attach` first.
