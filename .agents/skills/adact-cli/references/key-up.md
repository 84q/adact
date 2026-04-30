# `adact key-up`

Release a single key previously pressed by `adact key-down`. No snapshot is
captured.

## Synopsis

```
adact key-up <key> [--server <url>]
```

| Argument | Purpose                                                  |
| -------- | -------------------------------------------------------- |
| `<key>`  | Single key name (must match the one used by `key-down`). |

## Examples

```
adact key-down Control
adact key-down KEY_C
adact key-up KEY_C
adact key-up Control
```

## Error recovery

- Always release every key you press to avoid leaving the system in a
  modifier-stuck state.
