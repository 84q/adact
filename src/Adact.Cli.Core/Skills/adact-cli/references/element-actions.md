# Element action commands

Use this for: `click`, `doubleclick`, `hover`, `fill`, `type`, `focus`, `clear`, `scroll`.

## Shared rules

- Most commands target an element ref `s<sid>e<eid>` from the latest snapshot.
- Actions generally auto-capture a post-action snapshot unless `--no-snapshot` is used.
- If `REF_NOT_FOUND`, run `adact snapshot` and re-derive the ref.

## Command quick map

- `adact click <ref>`: single click (or multi-click via `--count`).
- `adact doubleclick <ref>`: double-click shorthand.
- `adact hover <ref>`: move pointer over target element.
- `adact fill <ref> --value "..."`: replace input value.
- `adact type <ref> --text "..."`: type text as key events.
- `adact focus <ref>`: set keyboard focus.
- `adact clear <ref>`: clear editable value.
- `adact scroll <ref>`: request scrolling until element becomes visible.
