# `adact scroll-into-view`

Scroll the element into the visible region using ScrollItemPattern. Low-level
command: no snapshot is captured.

## Synopsis

```
adact scroll-into-view <ref> [--server <url>]
```

## Examples

```
adact scroll-into-view s1e42
adact click s1e42
```

## Error recovery

- `ELEMENT_INTERACTION_FAILED` — the element does not support
  ScrollItemPattern. Use `adact mouse-wheel` on the surrounding scroll
  container instead.
