# `adact key-down`

Low-level command that presses and holds a single key on the active
session's window. Pair with `adact key-up` to release. No snapshot is
captured.

## Synopsis

```
adact key-down <key> [--server <url>]
```

| Argument | Purpose                                                          |
| -------- | ---------------------------------------------------------------- |
| `<key>`  | Single key name (e.g. `Shift`, `A`, `F1`). `+` combos forbidden. |

## Examples

```
adact key-down Shift
adact key-down A
adact key-up A
adact key-up Shift
```

## Error recovery

- `INVALID_ARGUMENT` — the key cannot be parsed or is a combo.
- Always release every key you press, otherwise the input device stays in a
  modifier-stuck state.
