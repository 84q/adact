# `adact type`

Focus an element and type the given text character by character. Use
`adact fill` for an atomic value-pattern set; use `type` when the target
needs key-by-key events (IME, masked inputs, etc.). A snapshot is captured
automatically.

## Synopsis

```
adact type <ref> <text> [--delay-ms <n>] [--no-snapshot]
                        [--snapshot-dir <dir>] [--server <url>]
```

| Argument / Flag  | Purpose                                                         |
| ---------------- | --------------------------------------------------------------- |
| `<ref>`          | Element ref in `s<sid>e<eid>` form.                             |
| `<text>`         | Text to type.                                                   |
| `--delay-ms`     | Delay between characters in milliseconds (>= 0). 0 = no delay.  |
| `--no-snapshot`  | Skip the automatic post-action snapshot.                        |

## Examples

```
adact type s1e5 "hello world"
adact type s1e5 "secret" --delay-ms 50
```

## Error recovery

- `REF_NOT_FOUND` — refresh with `adact snapshot` and retry.
- `ELEMENT_INTERACTION_FAILED` — the element does not accept keyboard input
  (read-only, disabled). Verify the target with `adact inspect` once it is
  available.
