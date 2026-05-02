# `adact mouse-wheel`

Scroll the mouse wheel at a target. `--delta-y > 0` scrolls down, `--delta-x
> 0` scrolls right. A snapshot is captured automatically (skip with
`--no-snapshot`).

## Synopsis

```
adact mouse-wheel <target> [--delta-y <n>] [--delta-x <n>]
                           [--no-snapshot] [--snapshot-dir <dir>]
                           [--server <url>]
```

| Argument / Flag  | Purpose                                                      |
| ---------------- | ------------------------------------------------------------ |
| `<target>`       | Either an element ref or `x,y` coordinates.                  |
| `--delta-y`      | Vertical scroll amount in notches (positive = down).         |
| `--delta-x`      | Horizontal scroll amount in notches (positive = right).      |
| `--no-snapshot`  | Skip the post-action snapshot.                               |

## Examples

```
adact mouse-wheel s1e2 --delta-y 3
adact mouse-wheel 400,500 --delta-y -2 --delta-x 1
```

## Error recovery

- `REF_NOT_FOUND` — the ref no longer exists; refresh with `adact snapshot`.
- `NO_ACTIVE_SESSION` — coordinate-based scroll requires an attached session.
