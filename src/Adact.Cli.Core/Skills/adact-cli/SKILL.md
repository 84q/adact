---
name: adact-cli
description: Use when the user asks to automate, drive, script, or test a Windows desktop GUI application (Notepad, Calculator, File Explorer, WPF/WinForms/Win32 apps) via UI Automation — including listing top-level windows, attaching to a window, capturing UIA snapshots, clicking buttons or menu items, or filling text into edit controls. ADACT (`adact`) is the CLI/MCP front end for these operations and identifies controls by stable element refs (`s<sid>e<eid>`).
---

# ADACT CLI

ADACT is a command-line tool that drives Windows desktop applications via UI
Automation (UIA). Each subcommand wraps a single MCP tool exposed by the ADACT
daemon, and prints results as line-oriented `key value` pairs or TSV that are
easy to parse.

## Workflow

1. **Find the target window** with `list-windows`.
2. **Attach** to it with `attach`, which returns a `sessionId`, a `windowRef`
   and the path to an automatically-captured snapshot.
3. **Read the snapshot** to locate the element you want to interact with and
   take its `ref` (an element ref of the form `s<sid>e<eid>`, e.g. `s1e7`).
4. **Act** with `click` or `fill`. Both commands automatically re-capture a
   snapshot after the action so you can decide on the next step.
5. Repeat steps 3-4 against the most recent snapshot until the task is done.

If the same window stays open between actions, element refs are reused across
snapshots, so a ref obtained earlier usually keeps working until the underlying
element disappears.

## Subcommands

Reference docs are grouped by command family under `references/`. Load the
family doc that covers the command you are about to run.

| Command                | Purpose                                               | Reference                                  |
| ---------------------- | ----------------------------------------------------- | ------------------------------------------ |
| `adact list-windows`   | List top-level windows on this desktop.               | [`references/session-bootstrap.md`](references/session-bootstrap.md) |
| `adact attach`         | Attach to a window and start a session.               | [`references/session-bootstrap.md`](references/session-bootstrap.md) |
| `adact launch`         | Start a Windows process (Win32 / .NET / UWP).         | [`references/session-bootstrap.md`](references/session-bootstrap.md) |
| `adact wait-for-window`| Wait until a top-level window appears (no attach).    | [`references/session-bootstrap.md`](references/session-bootstrap.md) |
| `adact snapshot`       | Capture a fresh UIA snapshot of the active session.   | [`references/snapshots-and-inspection.md`](references/snapshots-and-inspection.md) |
| `adact inspect`        | Print detailed UIA properties of an element as JSON.  | [`references/snapshots-and-inspection.md`](references/snapshots-and-inspection.md) |
| `adact screenshot`     | Save a PNG screenshot of the window or an element.    | [`references/snapshots-and-inspection.md`](references/snapshots-and-inspection.md) |
| `adact click`          | Click an element identified by an element ref.        | [`references/element-actions.md`](references/element-actions.md) |
| `adact fill`           | Overwrite an input element with the given text.       | [`references/element-actions.md`](references/element-actions.md) |
| `adact doubleclick`    | Double-click an element.                              | [`references/element-actions.md`](references/element-actions.md) |
| `adact hover`          | Move the cursor over an element.                      | [`references/element-actions.md`](references/element-actions.md) |
| `adact type`           | Type text character by character into an element.     | [`references/element-actions.md`](references/element-actions.md) |
| `adact focus`          | Set keyboard focus to an element.                     | [`references/element-actions.md`](references/element-actions.md) |
| `adact scroll-into-view` | Scroll an element into view (ScrollItemPattern).   | [`references/element-actions.md`](references/element-actions.md) |
| `adact scroll`         | Scroll a container element (ScrollPattern: percent/small/large). | [`references/element-actions.md`](references/element-actions.md) |
| `adact mousemove`      | Move the cursor to an element ref or `x,y`.           | [`references/mouse-and-keyboard.md`](references/mouse-and-keyboard.md) |
| `adact mousedown`      | Press and hold a mouse button at a target.            | [`references/mouse-and-keyboard.md`](references/mouse-and-keyboard.md) |
| `adact mouseup`        | Release a mouse button at a target.                   | [`references/mouse-and-keyboard.md`](references/mouse-and-keyboard.md) |
| `adact mousewheel`     | Scroll the mouse wheel at a target.                   | [`references/mouse-and-keyboard.md`](references/mouse-and-keyboard.md) |
| `adact keypress`       | Press a key combo (e.g. `Ctrl+C`).                    | [`references/mouse-and-keyboard.md`](references/mouse-and-keyboard.md) |
| `adact keydown`        | Press and hold a single key.                          | [`references/mouse-and-keyboard.md`](references/mouse-and-keyboard.md) |
| `adact keyup`          | Release a single key.                                 | [`references/mouse-and-keyboard.md`](references/mouse-and-keyboard.md) |
| `adact check`          | Ensure a checkbox / toggle / radio is On.             | [`references/selection-and-state.md`](references/selection-and-state.md) |
| `adact uncheck`        | Ensure a checkbox / toggle is Off.                    | [`references/selection-and-state.md`](references/selection-and-state.md) |
| `adact select`         | Select item(s) in a list/combo-box by name / index / ref. Supports `--add` / `--remove` for multi-select. | [`references/selection-and-state.md`](references/selection-and-state.md) |
| `adact wait-for-element` | Wait until an element reaches a target state.       | [`references/selection-and-state.md`](references/selection-and-state.md) |
| `adact resize-window`  | Resize the attached window.                           | [`references/window-and-lifecycle.md`](references/window-and-lifecycle.md) |
| `adact minimize-window`| Minimize the attached window.                         | [`references/window-and-lifecycle.md`](references/window-and-lifecycle.md) |
| `adact maximize-window`| Maximize the attached window.                         | [`references/window-and-lifecycle.md`](references/window-and-lifecycle.md) |
| `adact restore-window` | Restore the attached window to normal state.          | [`references/window-and-lifecycle.md`](references/window-and-lifecycle.md) |
| `adact detach`         | Detach the session without closing the window.        | [`references/window-and-lifecycle.md`](references/window-and-lifecycle.md) |
| `adact close-window`   | Close the attached window (auto-detach on success).   | [`references/window-and-lifecycle.md`](references/window-and-lifecycle.md) |
| `adact kill`           | Force-kill the attached process (auto-detach on success). | [`references/window-and-lifecycle.md`](references/window-and-lifecycle.md) |


## Element refs

Element refs use the format `s<sid>e<eid>` (for example `s1e7`):

- `<sid>` — session id digits (matches the session returned by `attach`).
- `<eid>` — element id assigned inside that session.

The CLI validates the format locally and rejects malformed refs with
`INVALID_REF_FORMAT` (exit 2) before contacting the daemon.

Window refs (returned by `list-windows`) use a simpler `w<n>` format and are only
valid as the first positional argument of `attach`.

## Output conventions

- **stdout** — line-oriented `key value` pairs, or TSV from `list-windows`. The
  fields you typically need are `sessionId`, `windowRef` and `snapshot` (path
  to the snapshot text file written under `.adact/` by default).
- **stderr** — error reports as:

  ```
  error <CODE>
  message <human-readable text>
  hint <optional follow-up suggestion>
  ```

- **exit code** — `0` success, `1` command failed, `2` user/argument error,
  `3` connection failed, `4` environment not supported (no interactive session).

## Common error recovery

| Error code                    | Meaning                                                | Recovery                                                                                          |
| ----------------------------- | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------- |
| `INVALID_ARGUMENT`            | Missing or conflicting CLI arguments.                  | Reread the reference doc and supply the required argument combination.                            |
| `INVALID_REF_FORMAT`          | Element ref does not match `s<sid>e<eid>`.             | Use a ref copied verbatim from the latest snapshot.                                               |
| `WINDOW_NOT_FOUND`            | Daemon could not attach to the HWND for the given `windowRef`. | Re-run `adact list-windows` and confirm the target window is still open.                  |
| `REF_NOT_FOUND`               | The element behind the ref is no longer reachable.     | Run `adact snapshot`, locate the element again from the new snapshot, then retry with the new ref. |
| `ELEMENT_INTERACTION_FAILED`  | Click/fill could not be performed on the element.      | Make sure the window is foreground and the control is enabled and on-screen; re-snapshot and retry. |
| `CONNECTION_FAILED`           | Could not reach the ADACT daemon.                      | Start the daemon with `adact serve`, or pass `--server <url>`.                                    |
| `OPERATION_BLOCKED`           | Desktop locked, UAC prompt, or window not foreground.  | Unlock the desktop, dismiss any UAC/system dialog, and ensure the window is in the foreground.    |
| `WAIT_TIMEOUT`                | `wait-for-element` / `wait-for-window` timed out.      | Increase `--timeout`, verify the app reaches the expected state, or relax the search conditions.  |

For the full list of all 19 error codes, see [`references/error-codes.md`](references/error-codes.md).

`REF_NOT_FOUND` is the most frequent error during automation. It means the
element the ref pointed to has gone (replaced, virtualized, dialog closed,
etc.). Always recover by capturing a new snapshot and re-deriving the ref
from the control's role/name/AutomationId, **not** by reusing the previous
ref.

## Snapshot output format

Transient popup UI (tooltips, menus, context menus, dialog boxes) may appear as
separate top-level windows near the top of the snapshot tree. This is expected:
Windows UIA often exposes these as distinct windows (for example a popup menu
window layered above the app window). When this happens, inspect those extra
window nodes and use refs from the currently visible popup subtree.

The `snapshot` command writes the full snapshot text to **stdout** as well as
to a file. Other commands (`click`, `fill`, `hover`, `type`, `keypress`, etc.)
only write the snapshot to a file and print the file path on stdout.

### `snapshot` command stdout

```
sessionId s1
snapshot .adact/session-1-20260428T180001234.txt
unchanged true      <-- only present when the UI state did not change
---
filter: operable
sessionId: s1
...
---
- Window "..." [ref=s1e1]
  ...
```

The key-value header (`sessionId`, `snapshot`, optionally `unchanged`) is
followed by the full snapshot text. AI clients can parse the header lines and
then use the tree body to locate element refs.

### Other commands (file only)

`click`, `fill`, and similar commands output only the header lines:

```
sessionId s1
snapshot .adact/session-1-20260428T180002500.txt
unchanged true      <-- only present when the UI state did not change
```

## Connecting to the daemon

All subcommands optionally accept `--server <url>`. With no flag they look at
`./.adact/config.json` and finally fall back to `http://127.0.0.1:41300/mcp`.

To start a local daemon: `adact serve` (HTTP) or `adact local` (stdio MCP).
