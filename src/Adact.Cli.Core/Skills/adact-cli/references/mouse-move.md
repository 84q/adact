# `adact mouse-move`

Low-level command that moves the mouse cursor to an element ref or absolute
screen coordinates. No snapshot is captured.

## Synopsis

```
adact mouse-move <target> [--server <url>]
```

| Argument | Purpose                                                       |
| -------- | ------------------------------------------------------------- |
| `<target>` | Either an element ref (`s<sid>e<eid>`) or `x,y` coordinates. |

## Examples

```
adact mouse-move s1e7
adact mouse-move 320,240
```

## Error recovery

- `INVALID_ARGUMENT` — the target is not a valid ref or `x,y` pair.
- `REF_NOT_FOUND` — the ref does not exist; refresh with `adact snapshot`.
- `NO_ACTIVE_SESSION` — coordinate-based move requires an attached session.
