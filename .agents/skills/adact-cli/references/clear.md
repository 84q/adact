# `adact clear`

Clear the value of an input element. Equivalent to `adact fill <ref> ""`.
A snapshot is captured automatically.

## Synopsis

```
adact clear <ref> [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

## Examples

```
adact clear s1e5
```

## Error recovery

- `ELEMENT_INTERACTION_FAILED` — the element is read-only or does not
  expose ValuePattern. Use `adact press` with `Ctrl+A` then `Delete` as a
  fallback.
