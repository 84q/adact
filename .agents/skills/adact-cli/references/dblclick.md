# `adact dblclick`

Double-click an element identified by an element ref. ADACT automatically
captures a fresh snapshot after the action.

## Synopsis

```
adact dblclick <ref> [--button <left|right|middle>]
                     [--modifier <key>]... [--position <x,y>]
                     [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

| Argument / Flag  | Purpose                                                                |
| ---------------- | ---------------------------------------------------------------------- |
| `<ref>`          | Element ref in `s<sid>e<eid>` form.                                    |
| `--button`       | Mouse button: `left` (default), `right`, or `middle`.                  |
| `--modifier`     | Modifier key held during the action (repeatable).                      |
| `--position`     | Click point relative to element top-left as `x,y`.                     |
| `--no-snapshot`  | Skip the automatic post-action snapshot.                               |
| `--snapshot-dir` | Override the snapshot output directory.                                |

## Examples

```
adact dblclick s1e9
adact dblclick s1e9 --modifier Shift
```

## Error recovery

- `REF_NOT_FOUND` — the element no longer exists. Run `adact snapshot` to get
  a fresh tree and retry with the new ref.
- `ELEMENT_INTERACTION_FAILED` — the element cannot be invoked (offscreen,
  disabled, or covered). Bring the window forward and retry.
