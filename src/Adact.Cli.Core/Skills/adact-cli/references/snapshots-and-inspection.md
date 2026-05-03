# Snapshot and inspection commands

Use this for: `snapshot`, `inspect`, `screenshot`.

## `adact snapshot`

- Purpose: capture a fresh UIA tree and obtain valid element refs (`s<sid>e<eid>`).
- Output: header lines (`sessionId`, `snapshot`, optional `unchanged`) plus full tree text on stdout.
- `--filter operable` (default): AI-friendly subset; `--filter raw`: full tree.

## Popup and dialog behavior in snapshots

- Tooltips, menus, context menus, and dialog boxes can appear as separate top-level windows.
- In snapshots, these popup windows are often listed near the top and layered above the main app window.
- This is expected UIA behavior; treat popup window nodes as active interaction targets when visible.

## `adact inspect <ref>`

- Purpose: print detailed properties for one element as JSON.
- Use when snapshot text is insufficient (pattern support, raw property values, etc.).

## `adact screenshot`

- Purpose: save a PNG of window or specific element.
- Use for visual verification when tree text alone is ambiguous.
