# Command Flows

This document explains how `adact <subcommand>` moves from CLI parsing to MCP tools, stores, the Engine, and CLI output. See [../spec/cli.md](../spec/cli.md) and [../spec/mcp-tools.md](../spec/mcp-tools.md) for command and tool catalogs.

## Common flow

Most subcommands follow this path:

1. Parse CLI arguments.
2. Validate inputs.
3. Resolve the daemon connection.
4. Call the MCP daemon.
5. Dispatch to the MCP tool.
6. Resolve session/window state.
7. Perform the Engine operation.
8. Convert the MCP result to CLI output.

## `adact list-windows`

1. `Program` starts `ListWindowsCommand`.
2. The command resolves `--server` and connects through `AdactMcpClient`.
3. The CLI calls `adact_list_windows`.
4. `WindowsTools.ListAppsAsync()` takes the tool lock and calls `UiaEngine.ListWindowsAsync()`.
5. The Engine enumerates visible top-level windows and returns window info.
6. `WindowRefStore.SyncOrAssign()` allocates or reuses `w<n>` values.
7. The MCP result returns a `windows` array.
8. The CLI prints TSV output.

## `adact attach`

`attach` identifies the target window by `windowRef` (`w<n>`). On success it registers the `WindowSession` and associates the `windowRef` with a `sessionId`.

1. Validate that the positional ref is a `w<n>` value.
2. Call `adact_attach`.
3. Resolve the `windowRef` through `WindowRefStore`.
4. Reuse an existing live session when possible.
5. Otherwise create a `WindowSession` from the HWND.
6. Register the session as `s<n>` and make it active.
7. Associate `w<n>` with `s<n>`.
8. Print `sessionId` and `windowRef`.
9. Unless `--no-snapshot` is set, auto-run `adact snapshot` and print the snapshot path.

## `adact snapshot`

1. Read `--sid`, `--filter`, and `--snapshot-dir`.
2. Call `adact_snapshot`.
3. Resolve the target session from `SessionStore`.
4. Build raw JSON through `WindowSession.SnapshotAsync()`.
5. Return raw JSON in the MCP response.
6. Parse, filter, format, and write the `.txt` snapshot on the CLI side.
7. Print `sessionId` and the snapshot path.

## `adact click` / `adact fill`

1. Validate the element ref.
2. Call `adact_click` or `adact_fill`.
3. Resolve the session from the ref prefix.
4. Resolve the current element from `RefRegistry`.
5. Execute the click or fill operation.
6. Run a short best-effort wait after the operation.
7. If allowed, auto-run `adact snapshot` and print the updated snapshot path.

## Wait and launch commands

- `wait-for-element` waits for a ref or query match and does not auto-snapshot.
- `wait-for-window` waits for a top-level window and does not attach.
- `launch` starts an app and returns only a PID-style result; follow up with `wait-for-window`, `list-windows`, and `attach` to interact with it.

## Lifecycle commands

`detach`, `close-window`, `kill`, and `daemon-stop` all use the same idea: resolve the target session, perform the action, clean up store associations, and print a small success result.

## `adact serve http` / `adact serve pipe`

1. Validate mode-specific options and connect Ctrl+C to cancellation.
2. Probe for an interactive desktop session before starting the listener.
3. Fail with `NO_INTERACTIVE_SESSION` if the session is not interactive.
4. For HTTP, bind Kestrel to `127.0.0.1:<port>`; for Named Pipe, build the pipe name from the workspace path and session.
5. Register `UiaEngine`, `SessionStore`, `WindowRefStore`, and the appropriate daemon control in DI.
6. Register `WindowsTools` with the MCP SDK or pipe listener.
7. Keep session/ref state in process memory while serving MCP tool calls.

## Related documents

| Document | Description |
| --- | --- |
| [class-responsibilities.md](class-responsibilities.md) | Class responsibilities and dependency direction |
| [snapshot-pipeline.md](snapshot-pipeline.md) | Snapshot and ref flow |
| [runtime-modes.md](runtime-modes.md) | CLI vs daemon runtime modes |
