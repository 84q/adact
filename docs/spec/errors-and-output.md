# Errors and Output

ADACT clearly separates success data from errors in both the CLI and MCP layers.

- The CLI writes normal success/failure information to stdout
- MCP business errors are returned as tool results with `isError: true`

## Exit codes

| Code | Name | Use |
| ---: | --- | --- |
| 0 | Success | Normal completion |
| 1 | CommandFailed | Tool error, operation failure, or internal failure reported by the daemon |
| 2 | UserError | CLI input errors, bad URL/config, remote `daemon-stop`, and similar cases |
| 3 | ConnectionFailed | Failed to connect to the daemon |
| 4 | EnvironmentNotSupported | The daemon started in an unsupported environment, currently `NO_INTERACTIVE_SESSION` |

## CLI stdout

### Common shape

```text
result: true|false
<optional metadata>
---
<body>
```

### Common failure shape

```text
result: false
error: <CODE>
---
message: <human-readable message>
hint: <optional recovery hint>
```

- Success uses `result: true`
- Failure uses `result: false` and `error: <CODE>`
- `hint` appears only when needed
- `serve http` and `serve pipe` are long-running commands and are not part of the normal unified result format

## CLI success formats

| Format | Commands | Content |
| --- | --- | --- |
| yaml-style | `attach`, action commands, lifecycle, `inspect`, `screenshot`, `wait-for-element`, `wait-for-window`, `launch`, `install`, `daemon-stop` | Metadata, then `---`, then a YAML-like body |
| TSV | `list-windows` | Metadata, then `---`, then TSV rows |
| snapshot | `snapshot` | `snapshotPath` in metadata, then `sessionId`, a blank line, and the tree |

## MCP tool errors

MCP business errors are returned as tool results rather than JSON-RPC errors.

| Field | Description |
| --- | --- |
| `isError` | `true` |
| `content[0].text` | `<CODE>: <message>` |
| `structuredContent.code` | Error code |
| `structuredContent.message` | Message |
| `structuredContent.details` | Optional details |

CLI clients convert `isError: true` into YAML-style CLI errors and usually return exit code `1`. Errors caught at the CLI input stage use exit code `2`.

## Representative error codes

| Code | Layer | Typical cause | Typical exit | Fix |
| --- | --- | --- | ---: | --- |
| `INVALID_ARGUMENT` | CLI / MCP | Missing argument, unknown filter, or unknown session id | 2 or 1 | Re-run with the correct arguments |
| `INVALID_REF_FORMAT` | CLI | Element ref is not `s<sid>e<eid>` | 2 | Copy the ref from the latest snapshot |
| `INVALID_WINDOW_REF` | MCP | `w<n>` is unknown or retired | 1 | Re-run `list-windows` |
| `WINDOW_NOT_FOUND` | MCP | HWND attach failed after resolving `windowRef` | 1 | Confirm the window still exists |
| `REF_NOT_FOUND` | MCP | Element ref is malformed, belongs to another session, or is not in the current snapshot | 1 | Re-snapshot and use a new ref |
| `ELEMENT_INTERACTION_FAILED` | MCP | Click/fill or similar UIA operation failed | 1 | Ensure the window is visible and retry |
| `SNAPSHOT_FAILED` | MCP | Snapshot construction failed | 1 | Re-attach and retry |
| `NO_ACTIVE_SESSION` | MCP | No active session exists | 1 | Run `attach` first |
| `NOT_FOUND` | MCP | The requested session does not exist | 1 | Create or select a valid session |
| `CLOSE_FAILED` | MCP | Window close failed | 1 | Close modal dialogs first |
| `KILL_FAILED` | MCP | Process kill failed | 1 | Confirm the process is still alive |
| `LAUNCH_FAILED` | Engine→MCP→CLI | Launch failed | 1 | Check the executable path or UWP identifier |
| `WAIT_TIMEOUT` | Engine→MCP→CLI | Wait command timed out | 1 | Increase the timeout or verify the app state |
| `CONNECTION_FAILED` | CLI | Could not connect to the daemon | 3 | Start `adact serve pipe` or specify `--server` |
| `ALREADY_RUNNING` | CLI | The daemon is already running | 2 | Reuse the existing daemon or stop it first |
| `LOCAL_ONLY` | CLI / MCP | Tried to stop a remote daemon | 2 or 1 | Run the command on the same host as the daemon |
| `OPERATION_BLOCKED` | Engine→MCP→CLI | Desktop is locked or blocked by UAC, etc. | 1 | Unlock the desktop and clear the blocking dialog |
| `NO_INTERACTIVE_SESSION` | Daemon startup | `adact serve http` / `adact serve pipe` started outside an interactive desktop session | 4 | Start the daemon from the interactive logon session |
| `INTERNAL_ERROR` | CLI / MCP | Unexpected internal failure | 1 | Retry; restart the daemon if it keeps happening |

## References

| Document | Description |
| --- | --- |
| [cli.md](cli.md) | CLI command output |
| [mcp-tools.md](mcp-tools.md) | MCP tool return values and error structure |
