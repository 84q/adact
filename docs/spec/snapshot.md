# Snapshot Specification

ADACT snapshots are treated as raw JSON in the Engine/MCP layer and as human-readable `.txt` files in the CLI layer. Since Phase 7, filtering and formatting are the CLI's responsibility.

## Responsibility split

| Layer | Input | Output | Responsibility |
| --- | --- | --- | --- |
| Engine | UIA tree | Raw JSON | Emit UIA properties and children with minimal loss |
| MCP Common | Engine raw JSON | MCP response | Return raw JSON as-is |
| CLI parser | MCP raw JSON | DTO | Convert `_meta` and `tree` into `SnapshotElement` objects |
| CLI filter | DTO | Filtered DTO | Apply the `operable` / `raw` tree filter |
| CLI formatter | Filtered DTO | Playwright-Aria-style text | Create a `.txt` file that is easy to read and operate on |
| CLI writer | Text | `.txt` file | Save under `.adact/` or `--snapshot-dir` |

## Engine / MCP raw JSON

Raw JSON has this top-level structure:

| Field | Description |
| --- | --- |
| `_meta` | Snapshot metadata |
| `tree` | Root UIA element |

Main `_meta` fields:

| Field | Description |
| --- | --- |
| `options.maxDepth` | Maximum traversal depth |
| `generatedAt` | UTC timestamp |
| `sessionId` | `s<n>` |
| `windowTitle` | Attached window title |
| `processName` | Process name |
| `processId` | Process id |
| `modalDialog` | Summary of detected modal dialogs, or `null` |

Main `tree` node fields:

| Field | Description |
| --- | --- |
| `ref` | `s<sid>e<eid>` |
| `role` | UIA ControlType |
| `name` | UIA Name |
| `automationId` | UIA AutomationId |
| `className` | Class name |
| `isEnabled` | Enabled state |
| `isOffscreen` | Offscreen state |
| `value` | ValuePattern or similar value |
| `helpText` | Help text |
| `boundingRect` | `[x, y, width, height]` |
| `isKeyboardFocusable` | Keyboard-focusable flag |
| `hasKeyboardFocus` | Focus flag |
| `isPopup` | Injected popup flag |
| `isModalDialog` | Injected modal-dialog flag |
| `children` | Child nodes |

## CLI `.txt` snapshot

The CLI receives raw JSON and formats it into frontmatter plus a Playwright-Aria-style tree.

```text
---
filter: operable
sessionId: s1
processName: ApplicationFrameHost
processId: 10392
generatedAt: "2026-04-28T01:00:54.4221919Z"
---
- Window "Calculator" [ref=s1e1]
  - Window "Calculator" [aid="TitleBar"] [value="Calculator"] [ref=s1e2]
    - Button "Close Calculator" [aid="Close"] [ref=s1e7]
```

### Frontmatter

| Field | Description |
| --- | --- |
| `filter` | `operable` or `raw` |
| `sessionId` | `s<n>` |
| `processName` | Process name when available |
| `processId` | Process id when available |
| `generatedAt` | Timestamp from the raw metadata |

Scalar values are quoted only when needed.

### Body format

| Item | Format |
| --- | --- |
| Line | `- Role "Name" [attr=...]` |
| Indent | 2 spaces |
| Name | Quoted when present |
| AutomationId | `[aid="..."]` |
| Value | `[value="..."]` |
| State | `[disabled]`, `[focused]`, `[modal]` |
| Ref | `[ref=s1e7]` |

Attribute order is `aid`, `value`, state flags, then `ref`.

## `operable` / `raw` filters

| Filter | Behavior |
| --- | --- |
| `raw` | Keep the tree structure intact |
| `operable` | Keep useful controls, flatten anonymous structural nodes, and drop offscreen subtrees |

Main roles preserved by `operable` include `Window`, `Menu`, `MenuBar`, `MenuItem`, `TitleBar`, `ToolBar`, `StatusBar`, `Button`, `Edit`, `CheckBox`, `ComboBox`, `Tree`, `List`, `DataGrid`, `Document`, `Text`, and similar controls.

Anonymous `Pane`, `Group`, `Custom`, `Thumb`, `Image`, and `Separator` nodes are flattened unless they carry a name or automation id.

## Unicode / escaping

| Target | Policy |
| --- | --- |
| Non-ASCII text | Preserve it |
| Double quote | `\"` |
| Backslash | `\\` |
| Newline | `\n` |
| Tab | `\t` |
| Control characters | `\uXXXX` |

ADACT keeps Unicode text readable because it deals with Windows UI text.

## Output files

| Item | Description |
| --- | --- |
| Default directory | `.adact/` |
| Override | `--snapshot-dir <dir>` |
| Extension | `.txt` |
| Commands that write snapshots | `attach`, `snapshot`, and auto-snapshot commands such as `click`, `fill`, `doubleclick`, `hover`, `type`, `check`, `uncheck`, `select`, `resize-window`, `minimize-window`, `maximize-window`, and `restore-window` |
| Commands that do not write snapshots | Low-level helpers and read/sync commands such as `keypress`, `mousemove`, `mousedown`, `mouseup`, `mousewheel`, `keydown`, `keyup`, `focus`, `scroll`, `inspect`, `screenshot`, `wait-for-element`, `wait-for-window`, and `launch` |
| Suppression | `--no-snapshot` disables auto-snapshot for supported commands |

The old `.json` snapshot format is no longer used for CLI output.

## References

| Document | Description |
| --- | --- |
| [ref-ids.md](ref-ids.md) | `[ref=...]` format |
| [cli.md](cli.md) | `snapshot`, `--filter`, and `--snapshot-dir` |
