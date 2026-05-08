# Selection and state commands

Use this for: `check`, `uncheck`, `select`, `wait-for-element`.

## `adact check <ref>` / `adact uncheck <ref>`

- Purpose: enforce On/Off state for checkable controls.
- Idempotent behavior: already-correct state is treated as success.

## `adact select`

- Purpose: select list/combo/tab item(s) by supported selector (name/index/ref).
- Use when explicit item choice is needed instead of generic click.
- Supports multiple items: `--name "A" --name "B"` or `--index 0 --index 2`.
- Only one selector kind per invocation (mixing `--name` with `--index` is an error).

### Selection modes

| Flag         | Behaviour                                                                   |
| ------------ | --------------------------------------------------------------------------- |
| *(default)*  | Replace: first item `Select()`, subsequent `AddToSelection()`. Clears others. |
| `--add`      | Add to existing selection (`AddToSelection()` for all items).                |
| `--remove`   | Remove from existing selection (`RemoveFromSelection()` for all items).      |

- `--add` and `--remove` cannot be combined.
- `--add` / `--remove` require the container to support multi-select (`CanSelectMultiple`). If unsupported, the command returns `ELEMENT_INTERACTION_FAILED`.

## `adact wait-for-element`

- Purpose: poll until an element reaches a target state (`visible`, `hidden`, `enabled`, `disabled`, etc.).
- Modes: ref mode (`--ref`) or search-condition mode (`--name`/`--control-type`/...).
- Timeout errors return `WAIT_TIMEOUT`; increase `--timeout` or relax conditions.
