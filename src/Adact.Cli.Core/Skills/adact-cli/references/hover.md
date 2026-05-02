# `adact hover`

Move the mouse cursor over an element. Useful for revealing tooltips or
hover-only menus. A snapshot is automatically captured afterwards.

## Synopsis

```
adact hover <ref> [--modifier <key>]... [--position <x,y>]
                  [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

| Argument / Flag  | Purpose                                                          |
| ---------------- | ---------------------------------------------------------------- |
| `<ref>`          | Element ref in `s<sid>e<eid>` form.                              |
| `--modifier`     | Modifier key held during hover (repeatable).                     |
| `--position`     | Hover point relative to element top-left as `x,y`.               |
| `--no-snapshot`  | Skip the automatic post-action snapshot.                         |

## Examples

```
adact hover s1e3
adact hover s1e3 --position 10,5
```

## Error recovery

- `REF_NOT_FOUND` — re-run `adact snapshot` and use the new ref.
