# `adact uncheck`

Ensure a checkbox / toggle is in the Off (unselected) state. Idempotent. A
snapshot is captured automatically.

## Synopsis

```
adact uncheck <ref> [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

## Examples

```
adact uncheck s1e8
```

## Error recovery

- `ELEMENT_INTERACTION_FAILED` — the element does not support uncheck (e.g.
  a radio button can only be selected). Pick a different target.
