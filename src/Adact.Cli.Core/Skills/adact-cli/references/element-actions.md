# Element action commands

Use this for: `click`, `doubleclick`, `hover`, `fill`, `type`, `focus`, `scroll-into-view`, `scroll`.

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
- `adact scroll-into-view <ref>`: scroll element into view (ScrollItemPattern).
- `adact scroll <ref> --percent-v 50`: scroll container (percent/small/large modes, mutually exclusive).
