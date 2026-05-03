# Mouse and keyboard commands

Use this for: `mouse-move`, `mouse-down`, `mouse-up`, `mouse-wheel`, `press`, `key-down`, `key-up`.

## Mouse

- `adact mouse-move <ref|x,y>`: move cursor to element or screen coordinate.
- `adact mouse-down <ref|x,y> [--button ...]`: press and hold mouse button.
- `adact mouse-up <ref|x,y> [--button ...]`: release mouse button.
- `adact mouse-wheel <ref|x,y> --delta <n>`: wheel scroll at target.

## Keyboard

- `adact press --key "Ctrl+S"`: press a key chord.
- `adact key-down --key Shift`: hold one key.
- `adact key-up --key Shift`: release one key.

## Typical drag flow

```bash
adact mouse-move s1e10
adact mouse-down s1e10 --button left
adact mouse-move s1e24
adact mouse-up s1e24 --button left
```
