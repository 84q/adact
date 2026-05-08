# CLI Specification

ADACT's primary interface is the `adact <subcommand>` CLI. It starts as a short-lived process and connects by default to the MCP daemon over Named Pipe (`adact serve pipe`).

## Unified output rules

Normal subcommands, except `serve http` and `serve pipe`, write stdout in one of these forms:

```text
result: true|false
<optional metadata>
---
<body>
```

- Success uses `result: true`
- Failure uses `result: false` and `error: <ERROR_CODE>`
- Normal success/failure information goes to stdout; stderr is reserved for exceptional cases
- `windowRef` is no longer part of the normal success output and appears only in `list-windows` TSV rows
- `sessionId` is not placed in the metadata block; it appears in the body when needed

## Format categories

| Format | Commands | Notes |
| --- | --- | --- |
| TSV | `list-windows` | TSV body after the metadata block |
| snapshot | `snapshot` | `snapshotPath` in metadata, then `sessionId`, a blank line, and the tree body |
| yaml-style | All other one-shot commands | YAML-like body after the metadata block |
| excluded | `serve http`, `serve pipe` | Long-running servers, so they are excluded from the unified result format |

## Command list

| Category | Commands | Format | Summary |
| --- | --- | --- | --- |
| Runtime | `serve http` | excluded | Start the HTTP MCP daemon |
| Runtime | `serve pipe` | excluded | Start the Named Pipe MCP daemon |
| Discovery | `list-windows` | TSV | List top-level windows |
| Session | `attach` | yaml-style | Attach to a window and create a session |
| Snapshot | `snapshot` | snapshot | Save and/or print the snapshot for the active or selected session |
| Mouse | `click`, `doubleclick`, `hover` | yaml-style | UI actions with auto-snapshot support |
| Mouse | `mousemove`, `mousedown`, `mouseup`, `mousewheel` | yaml-style | Low-level mouse actions |
| Keyboard | `fill`, `type` | yaml-style | UI actions with auto-snapshot support |
| Keyboard | `keypress`, `keydown`, `keyup` | yaml-style | Low-level keyboard actions |
| Toggle | `check`, `uncheck`, `select` | yaml-style | UI actions with auto-snapshot support |
| Toggle | `focus`, `scroll-into-view`, `scroll` | yaml-style | Helper operations |
| Window | `resize-window`, `minimize-window`, `maximize-window`, `restore-window` | yaml-style | Window state changes with auto-snapshot support |
| Inspect | `inspect` | yaml-style | UIA property details |
| Inspect | `screenshot` | yaml-style | Save PNG |
| Wait | `wait-for-element` | yaml-style | Wait for element state |
| Wait | `wait-for-window` | yaml-style | Wait for a top-level window |
| Lifecycle | `launch` | yaml-style | Start a Win32/.NET/UWP process |
| Lifecycle | `detach`, `close-window`, `kill` | yaml-style | Session/window lifecycle operations |
| Lifecycle | `daemon-stop` | yaml-style | Stop the Named Pipe daemon |
| Install | `install --skills` | yaml-style | Expand Skill files |

## Representative output

### `attach`

```text
result: true
snapshotPath: .adact/snapshots/s1/0001.txt (changed)
---
sessionId: s1
processId: 12345
title: Untitled - Notepad
```

### `attach --no-snapshot`

```text
result: true
---
sessionId: s1
processId: 12345
title: Untitled - Notepad
```

### `snapshot`

```text
result: true
snapshotPath: .adact/snapshots/s1/0012.txt (changed)
---
sessionId: s1

- Window "Untitled - Notepad" [ref=s1e1]
  - Edit [ref=s1e2]
```

### `list-windows`

```text
result: true
---
windowRef	sessionId	processName	processId	className	windowTitle
w1	s1	notepad	12345	Notepad	Untitled - Notepad
```

### Common failure

```text
result: false
error: NO_ACTIVE_SESSION
---
message: No active session. Call adact_attach first or specify sessionId explicitly.
```

## Auto-snapshot commands

`attach`, `click`, `fill`, `doubleclick`, `hover`, `type`, `check`, `uncheck`, `select`, `resize-window`, `minimize-window`, `maximize-window`, and `restore-window` can auto-capture a snapshot on success.

- When `--no-snapshot` is set, `snapshotPath` is omitted
- Only the `snapshot` command prints the snapshot body to stdout

## Session-id interface

Some commands accept session selection as a positional `sid` argument rather than `--sid`.

- `snapshot`, `resize-window`, `minimize-window`, `maximize-window`, `restore-window`, `close-window`, and `kill`
- When omitted, they resolve the active session

`screenshot` accepts a positional `target` argument and auto-detects it:

- `s<digits>e<digits>` means element ref
- Otherwise it is treated as a session id
- If omitted, the active session is used

## Runtime commands

| Command | Main arguments | Notes |
| --- | --- | --- |
| `adact serve http` | `--port <0-65535>` | Long-running; excluded from the unified output format |
| `adact serve pipe` | none | Long-running; excluded from the unified output format |

`serve http` and `serve pipe` must run inside an interactive desktop session. In non-interactive sessions they fail with `NO_INTERACTIVE_SESSION` and exit code `4`.

## Connection resolution

| Priority | Input | Example |
| ---: | --- | --- |
| 1 | `--server` | `adact list-windows --server http://127.0.0.1:41300/mcp` |
| 2 | Named Pipe (default) | Derived automatically from the workspace path |

If `--server` is omitted, the CLI uses Named Pipe. Use `--server` explicitly for HTTP mode.

## `install --skills`

```powershell
adact install --skills <copilot|claude|codex> [--global]
```

| Item | Description |
| --- | --- |
| Purpose | Expand Skill files for AI coding clients |
| Input | `--skills copilot|claude|codex` is required; `--global` installs for the current user |
| Output | YAML-style `{ installed: true, skills, path }` |
| Target skills | `src/Adact.Cli.Core/Skills/{adact-cli,adact-flaui-testgen}/` |
| Install location | The client-specific skills root under each Skill directory |

## References

| Document | Description |
| --- | --- |
| [errors-and-output.md](errors-and-output.md) | Exit codes and CLI/MCP output details |
| [snapshot.md](snapshot.md) | Snapshot file format |
| [ref-ids.md](ref-ids.md) | `w<n>`, `s<n>`, and `s<sid>e<eid>` |
