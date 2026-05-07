# Selection and state commands

Use this for: `check`, `uncheck`, `select`, `wait-for-element`.

## `adact check <ref>` / `adact uncheck <ref>`

- Purpose: enforce On/Off state for checkable controls.
- Idempotent behavior: already-correct state is treated as success.

## `adact select`

- Purpose: select list/combo/tab item by supported selector (name/index/ref).
- Use when explicit item choice is needed instead of generic click.

## `adact wait-for-element`

- Purpose: poll until an element reaches a target state (`visible`, `hidden`, `enabled`, `disabled`, etc.).
- Modes: ref mode (`--ref`) or search-condition mode (`--name`/`--control-type`/...).
- Timeout errors return `WAIT_TIMEOUT`; increase `--timeout` or relax conditions.
