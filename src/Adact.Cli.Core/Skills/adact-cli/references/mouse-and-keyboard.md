# Mouse and keyboard commands

Use this for: `mousemove`, `mousedown`, `mouseup`, `mousewheel`, `keypress`, `keydown`, `keyup`.

## Mouse

- `adact mousemove <ref|x,y>`: move cursor to element or screen coordinate.
- `adact mousedown <ref|x,y> [--button ...]`: press and hold mouse button.
- `adact mouseup <ref|x,y> [--button ...]`: release mouse button.
- `adact mousewheel <ref|x,y> --delta <n>`: wheel scroll at target.
  - At least one of `--delta-x` or `--delta-y` must be non-zero. Both zero returns `INVALID_ARGUMENT`.

## Keyboard

- `adact keypress --key "Ctrl+S"`: press a key chord.
- `adact keydown --key Shift`: hold one key.
- `adact keyup --key Shift`: release one key.

## Typical drag flow

```bash
adact mousemove s1e10
adact mousedown s1e10 --button left
adact mousemove s1e24
adact mouseup s1e24 --button left
```
