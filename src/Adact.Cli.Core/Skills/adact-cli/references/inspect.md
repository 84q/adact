# `adact inspect`

Get detailed UIA properties of a single element identified by ref. Useful when
the snapshot summary is not enough — for example, to read the current
`ToggleState`, `IsSelected`, `Value`, `BoundingRect`, or to confirm which
patterns the element supports before deciding which command to run.

`inspect` does not capture a snapshot.

## Synopsis

```
adact inspect --ref <ref> [--server <url>]
```

| Flag       | Purpose                                        |
| ---------- | ---------------------------------------------- |
| `--ref`    | Element ref to inspect (e.g. `s1e7`). Required. |
| `--server` | Daemon endpoint URL.                           |

## Output

A single JSON line on stdout:

```json
{"ref":"s1e7","name":"OK","controlType":"Button","automationId":"okBtn","className":"Button","helpText":null,"value":null,"boundingRect":{"x":120,"y":80,"width":80,"height":24},"isEnabled":true,"isOffscreen":false,"isKeyboardFocusable":true,"hasKeyboardFocus":false,"patterns":{}}
```

Fields:

- `ref`, `name`, `controlType`, `automationId`, `className`, `helpText` —
  basic UIA properties (string fields are `null` when empty).
- `value` — `ValuePattern.Value` (null when the element does not support
  `ValuePattern`).
- `boundingRect` — screen-coordinate rectangle `{x, y, width, height}`.
- `isEnabled`, `isOffscreen`, `isKeyboardFocusable`, `hasKeyboardFocus` —
  state flags.
- `patterns` — object whose keys are pattern names (`Toggle`,
  `SelectionItem`, `ExpandCollapse`, `RangeValue`, `Window`). Each value
  contains the pattern's current state, for example:
  - `Toggle`: `{"ToggleState":"On"}` (or `Off` / `Indeterminate`).
  - `SelectionItem`: `{"IsSelected":true}`.
  - `ExpandCollapse`: `{"ExpandCollapseState":"Collapsed"}` (or `Expanded`,
    `PartiallyExpanded`, `LeafNode`).
  - `RangeValue`: `{"Min":0,"Max":100,"Value":42}`.
  - `Window`: `{"VisualState":"Normal","InteractionState":"ReadyForUserInteraction"}`.

Patterns the element does not implement are simply absent from the object.

## Error recovery

- `INVALID_REF_FORMAT` — `--ref` was not in `s<sid>e<eid>` form.
- `REF_NOT_FOUND` — the element behind the ref is no longer reachable. Run
  `adact snapshot` and retry with a fresh ref.
