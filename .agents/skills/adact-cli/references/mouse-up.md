# `adact mouse-up`

Release a mouse button previously pressed by `adact mouse-down`. No snapshot
is captured.

## Synopsis

```
adact mouse-up <target> [--button <left|right|middle>] [--server <url>]
```

| Argument / Flag | Purpose                                                       |
| --------------- | ------------------------------------------------------------- |
| `<target>`      | Either an element ref or `x,y` coordinates.                   |
| `--button`      | Mouse button: must match the one used with `mouse-down`.      |

## Examples

```
adact mouse-down s1e3
adact mouse-move 200,300
adact mouse-up 200,300
```

## Error recovery

- Always release every button you press to avoid stuck-button state.
