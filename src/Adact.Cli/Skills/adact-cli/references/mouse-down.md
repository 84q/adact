# `adact mouse-down`

Press and hold a mouse button at a target. Pair with `adact mouse-up` to
implement drag-like sequences. No snapshot is captured.

## Synopsis

```
adact mouse-down <target> [--button <left|right|middle>] [--server <url>]
```

| Argument / Flag | Purpose                                                       |
| --------------- | ------------------------------------------------------------- |
| `<target>`      | Either an element ref (`s<sid>e<eid>`) or `x,y` coordinates.  |
| `--button`      | Mouse button: `left` (default), `right`, or `middle`.         |

## Examples

```
adact mouse-down s1e3
adact mouse-down 100,200 --button right
```

## Error recovery

- The cursor stays pressed until you call `adact mouse-up`. Always pair them
  to avoid leaving the system in a stuck-button state.
