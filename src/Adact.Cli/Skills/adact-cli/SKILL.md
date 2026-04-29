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

1. **Find the target window** with `list-apps`.
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

Each subcommand has a dedicated reference document under `references/`. Load
the reference for the command you are about to run.

| Command                | Purpose                                               | Reference                                  |
| ---------------------- | ----------------------------------------------------- | ------------------------------------------ |
| `adact list-apps`      | List top-level windows on this desktop.               | [`references/list-apps.md`](references/list-apps.md) |
| `adact attach`         | Attach to a window and start a session.               | [`references/attach.md`](references/attach.md)       |
| `adact snapshot`       | Capture a fresh UIA snapshot of the active session.   | [`references/snapshot.md`](references/snapshot.md)   |
| `adact click`          | Click an element identified by an element ref.        | [`references/click.md`](references/click.md)         |
| `adact fill`           | Overwrite an input element with the given text.       | [`references/fill.md`](references/fill.md)           |
| `adact dblclick`       | Double-click an element.                              | [`references/dblclick.md`](references/dblclick.md)   |
| `adact hover`          | Move the cursor over an element.                      | [`references/hover.md`](references/hover.md)         |
| `adact mouse-move`     | Move the cursor to an element ref or `x,y`.           | [`references/mouse-move.md`](references/mouse-move.md) |
| `adact mouse-down`     | Press and hold a mouse button at a target.            | [`references/mouse-down.md`](references/mouse-down.md) |
| `adact mouse-up`       | Release a mouse button at a target.                   | [`references/mouse-up.md`](references/mouse-up.md)   |
| `adact mouse-wheel`    | Scroll the mouse wheel at a target.                   | [`references/mouse-wheel.md`](references/mouse-wheel.md) |
| `adact press`          | Press a key combo (e.g. `Ctrl+C`).                    | [`references/press.md`](references/press.md)         |
| `adact key-down`       | Press and hold a single key.                          | [`references/key-down.md`](references/key-down.md)   |
| `adact key-up`         | Release a single key.                                 | [`references/key-up.md`](references/key-up.md)       |
| `adact type`           | Type text character by character into an element.     | [`references/type.md`](references/type.md)           |
| `adact check`          | Ensure a checkbox / toggle / radio is On.             | [`references/check.md`](references/check.md)         |
| `adact uncheck`        | Ensure a checkbox / toggle is Off.                    | [`references/uncheck.md`](references/uncheck.md)     |
| `adact select`         | Select a list / combo-box item by name / index / ref. | [`references/select.md`](references/select.md)       |
| `adact focus`          | Set keyboard focus to an element.                     | [`references/focus.md`](references/focus.md)         |
| `adact clear`          | Clear the value of an input element.                  | [`references/clear.md`](references/clear.md)         |
| `adact scroll-into-view` | Scroll an element into view.                        | [`references/scroll-into-view.md`](references/scroll-into-view.md) |
| `adact resize`         | Resize the attached window.                           | [`references/resize.md`](references/resize.md)       |
| `adact minimize`       | Minimize the attached window.                         | [`references/minimize.md`](references/minimize.md)   |
| `adact maximize`       | Maximize the attached window.                         | [`references/maximize.md`](references/maximize.md)   |
| `adact restore`        | Restore the attached window to normal state.          | [`references/restore.md`](references/restore.md)     |
| `adact launch`         | Start a Windows process (Win32 / .NET / UWP).         | [`references/launch.md`](references/launch.md)       |

## Element refs

Element refs use the format `s<sid>e<eid>` (for example `s1e7`):

- `<sid>` — session id digits (matches the session returned by `attach`).
- `<eid>` — element id assigned inside that session.

The CLI validates the format locally and rejects malformed refs with
`INVALID_REF_FORMAT` (exit 2) before contacting the daemon.

Window refs (returned by `list-apps`) use a simpler `w<n>` format and are only
valid as the first positional argument of `attach`.

## Output conventions

- **stdout** — line-oriented `key value` pairs, or TSV from `list-apps`. The
  fields you typically need are `sessionId`, `windowRef` and `snapshot` (path
  to the snapshot text file written under `.adact/` by default).
- **stderr** — error reports as:

  ```
  error <CODE>
  message <human-readable text>
  hint <optional follow-up suggestion>
  ```

- **exit code** — `0` success, `1` command failed, `2` user/argument error,
  `3` connection failed.

## Common error recovery

| Error code            | Meaning                                               | Recovery                                                                                          |
| --------------------- | ----------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `INVALID_ARGUMENT`    | Missing or conflicting CLI arguments.                 | Reread the reference doc and supply the required argument combination.                           |
| `INVALID_REF_FORMAT`  | Element ref does not match `s<sid>e<eid>`.            | Use a ref copied verbatim from the latest snapshot.                                               |
| `INVALID_WINDOW_REF`  | Window ref is well-formed but unknown / retired.      | Re-run `adact list-apps` and use a freshly printed `windowRef`.                                   |
| `WINDOW_NOT_FOUND`    | Daemon could not attach to the HWND for the given `windowRef`. | Re-run `adact list-apps` and confirm the target window is still open. |
| `REF_NOT_FOUND`       | The element behind the ref is no longer reachable.    | Run `adact snapshot`, locate the element again from the new snapshot, then retry with the new ref. |
| `ELEMENT_INTERACTION_FAILED` | Click/fill could not be performed on the element. | Make sure the window is foreground and the control is enabled and on-screen; re-snapshot and retry. |
| `NO_ACTIVE_SESSION`   | `snapshot` was called without an attached session.    | Call `adact attach` first, or pass `--sid` explicitly.                                            |
| `CONNECTION_FAILED`   | Could not reach the ADACT daemon.                     | Start the daemon with `adact serve`, or pass `--server <url>`.                                    |
| `LOCAL_ONLY`          | Operation only valid against a localhost daemon.      | Run the command on the same host as the daemon.                                                   |

`REF_NOT_FOUND` is the most frequent error during automation. It means the
element the ref pointed to has gone (replaced, virtualized, dialog closed,
etc.). Always recover by capturing a new snapshot and re-deriving the ref
from the control's role/name/AutomationId, **not** by reusing the previous
ref.

## Connecting to the daemon

All subcommands optionally accept `--server <url>`. With no flag they look at
`./.adact/config.json` and finally fall back to `http://127.0.0.1:41300/mcp`.

To start a local daemon: `adact serve` (HTTP) or `adact local` (stdio MCP).
