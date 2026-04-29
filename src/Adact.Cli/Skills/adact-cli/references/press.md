# `adact press`

Press a key combo such as `Ctrl+C`, `Enter`, or `F5`. If `--ref` is given,
that element is focused first; otherwise the active session's window is
used. A snapshot is captured automatically.

## Synopsis

```
adact press <key> [--ref <ref>] [--no-snapshot]
                  [--snapshot-dir <dir>] [--server <url>]
```

| Argument / Flag  | Purpose                                                       |
| ---------------- | ------------------------------------------------------------- |
| `<key>`          | Key combo (e.g. `Enter`, `F5`, `Ctrl+Shift+E`).               |
| `--ref`          | Optional element ref to focus before pressing.                |
| `--no-snapshot`  | Skip the automatic post-action snapshot.                      |

## Examples

```
adact press Enter
adact press Ctrl+S
adact press F5 --ref s1e3
```

## Error recovery

- `INVALID_ARGUMENT` — the key combo cannot be parsed (typo, unknown key).
- `NO_ACTIVE_SESSION` — no session is attached and `--ref` was not given.
  Run `adact attach` first.
