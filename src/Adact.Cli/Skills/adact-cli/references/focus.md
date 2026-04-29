# `adact focus`

Set keyboard focus to the element identified by ref. Low-level command: no
snapshot is captured.

## Synopsis

```
adact focus <ref> [--server <url>]
```

## Examples

```
adact focus s1e5
adact press Tab
```

## Error recovery

- `ELEMENT_INTERACTION_FAILED` — the element is not focusable (offscreen,
  disabled). Bring it into view with `adact scroll-into-view` or click it
  first.
