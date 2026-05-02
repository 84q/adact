# `adact check`

Ensure a checkbox / toggle / radio button is in the On (selected) state.
Idempotent: if the element is already On, nothing happens. A snapshot is
captured automatically.

## Synopsis

```
adact check <ref> [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

## Examples

```
adact check s1e8
```

## Error recovery

- `ELEMENT_INTERACTION_FAILED` — the element does not support the toggle
  pattern. Use `adact click` instead, or pick a different target.
