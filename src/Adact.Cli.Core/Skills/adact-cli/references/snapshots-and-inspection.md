# Snapshot and inspection commands

Use this for: `snapshot`, `inspect`, `screenshot`.

## `adact snapshot`

- Purpose: capture a fresh UIA tree and obtain valid element refs (`s<sid>e<eid>`).
- Output: header lines (`sessionId`, `snapshot`, optional `unchanged`) plus full tree text on stdout.
- `--filter operable` (default): AI-friendly subset; `--filter raw`: full tree.

## Popup and dialog behavior in snapshots

Popup/modal windows appear as separate top-level entries in snapshots.
See [`popup-and-modal.md`](popup-and-modal.md) for flags, interaction patterns, and modal constraints.

## `adact inspect <ref>`

- Purpose: print detailed properties for one element as JSON.
- Use when snapshot text is insufficient (pattern support, raw property values, etc.).

## `adact screenshot`

- Purpose: save a PNG of window or specific element.
- Use for visual verification when tree text alone is ambiguous.
