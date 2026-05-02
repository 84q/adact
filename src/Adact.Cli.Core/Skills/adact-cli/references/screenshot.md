# `adact screenshot`

Capture a PNG screenshot of the attached window or a specific element.
`screenshot` does not capture a UIA snapshot.

## Synopsis

```
adact screenshot [--ref <ref>] [--out <path>] [--sid <sessionId>] [--server <url>]
```

| Flag       | Purpose                                                                        |
| ---------- | ------------------------------------------------------------------------------ |
| `--ref`    | Element ref to clip (e.g. `s1e7`). Omit to capture the whole attached window.  |
| `--out`    | Output PNG path. Default: `./.adact/screenshot-<sid>-<UTC ts>.png`.            |
| `--sid`    | Target session id when `--ref` is omitted (default: active session).           |
| `--server` | Daemon endpoint URL.                                                           |

When `--ref` is given, the element's bounding rectangle on screen is used as
the clip region; otherwise the attached window's bounding rectangle is used.
The image format is PNG.

## Caveats

- The capture is performed via Win32 GDI (`Graphics.CopyFromScreen`). If the
  target window or element is partially or fully occluded by another window,
  the occluding pixels appear in the output. Bring the target window to the
  foreground (`adact restore` / OS window activation) before capturing if
  pixel-accurate output matters.
- `--out` must end with `.png` (case-insensitive). Other extensions are
  rejected with `INVALID_ARGUMENT`.

## Output

A single JSON line on stdout containing the saved path and image dimensions:

```json
{"path":"C:\\workspace\\.adact\\screenshot-1-20260428T180001234.png","width":640,"height":480}
```

## Error recovery

- `INVALID_REF_FORMAT` — `--ref` was provided but not in `s<sid>e<eid>` form.
- `INVALID_ARGUMENT` — `--out` extension is not `.png`. Rename the target.
- `REF_NOT_FOUND` — the element behind `--ref` is no longer reachable. Run
  `adact snapshot` and retry with a fresh ref.
- `ELEMENT_INTERACTION_FAILED` — the bounding rectangle is empty (the window
  is minimized, or the element is offscreen). Restore the window or scroll
  the element into view, then retry.
- `NO_ACTIVE_SESSION` — `--ref` was omitted and no session is attached. Call
  `adact attach` first or pass `--sid` explicitly.
