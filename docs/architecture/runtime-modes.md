# Runtime Modes

`adact.exe` / `src/Adact.Cli` is a Windows-targeted single executable with three modes: CLI client, HTTP MCP daemon, and Named Pipe MCP daemon.

## Mode comparison

| Mode | Command | Purpose | Typical user |
| --- | --- | --- | --- |
| CLI client | `adact <subcommand>` | Main path for connecting to a daemon and performing window operations | AI agents, humans |
| HTTP daemon | `adact serve http [--port <port>]` | Exposes MCP tools over `/mcp` and keeps UIA/session/ref state | HTTP MCP clients |
| Named Pipe daemon | `adact serve pipe` | Exposes MCP tools over Named Pipe and keeps UIA/session/ref state | CLI client (default target) |

## Windows target and future boundaries

The current single `adact.exe` is Windows-targeted because it includes Windows GUI/UIA operations. `serve http` and `serve pipe` use UIA directly, so they must run in the same interactive Windows session as the target GUI.

For a future cross-platform client, the GUI-independent CLI client could be separated from the Windows UIA implementation and multi-targeted so it can run from remote terminals on macOS or Linux. The daemon that reads the GUI would still remain on the Windows GUI session side.

## `adact <subcommand>`

The CLI client is short-lived. Each command resolves a target, connects to the daemon, and performs one operation.

| Item | Description |
| --- | --- |
| stdin | Usually unused |
| stdout | Machine-readable success output such as key-value, TSV, or snapshot paths |
| stderr | `error` / `message` / `hint` output, connection failures, and CLI input errors |
| State | The CLI does not keep state; session/ref data lives in daemon memory |
| Target | HTTP when `--server` is set, otherwise Named Pipe derived from the workspace path |

## `adact serve http`

`adact serve http` is the HTTP MCP daemon. It binds to the selected localhost port and exposes `/mcp`.

| Item | Description |
| --- | --- |
| stdin | Usually unused |
| stdout | Not used for data output |
| stderr | Daemon logs, interactive-session checks, and startup failures |
| State | `SessionStore` and `WindowRefStore` live in process memory |
| UIA requirement | Must run in the same interactive Windows session as the target GUI |

`serve http` reads the GUI through UIA. If it is started from SSH, a service, or a non-interactive session, the GUI window will not be visible. At startup it checks `WinSta0` and `SessionId`, and fails before starting the listener when the desktop is not interactive.

## `adact serve pipe`

`adact serve pipe` is the Named Pipe MCP daemon. It derives a unique pipe name from the workspace path and exchanges MCP traffic through that pipe.

| Item | Description |
| --- | --- |
| stdin | Usually unused |
| stdout | Not used for data output |
| stderr | Daemon logs, interactive-session checks, and startup failures |
| State | `SessionStore` and `WindowRefStore` live in process memory |
| UIA requirement | Must run in the same interactive Windows session as the target GUI |
| Pipe name | Auto-generated from the workspace path hash (`\\.\pipe\adact-<hash>-<session>`) |

Named Pipe mode is optimized for workspace-scoped CLI client connections. Security relies on Windows Named Pipe ACLs.

## Named Pipe internals

Named Pipe mode generates a unique pipe name from the workspace path.

### Pipe name generation

1. Search upward from the current directory for `.adact/`
2. Normalize and hash the discovered path
3. Build the pipe name from the hash and Windows session name: `\\.\pipe\adact-<hash>-<session>`

This makes the CLI client and daemon in the same workspace connect to the same pipe automatically.

### Connection flow

1. The CLI client runs a command without `--server`
2. `ConnectionResolver` resolves the Named Pipe endpoint
3. `NamedPipeMcpClient` connects to the pipe
4. If the connection fails, the daemon is started automatically (`DaemonSpawner`)
5. MCP JSON-RPC traffic is exchanged over the pipe

## Interactive session constraints

`serve http` and `serve pipe` use UIA directly, so they check for an interactive desktop at startup.

| Condition | Result |
| --- | --- |
| `SessionId == 0` | Startup refused |
| Window Station is not `WinSta0` | Startup refused |
| Inside an interactive logon session | Startup continues |

On failure, exit code `4` is returned and stderr looks like this:

```text
error NO_INTERACTIVE_SESSION
message daemon is not in an interactive desktop session (...)
hint launch the daemon from the interactive logon session that owns the target GUI windows
```

`adact serve pipe` uses the same exit code `4`, with a hint tailored for `adact serve pipe`.

## Operational notes

| Situation | Recommendation |
| --- | --- |
| AI / CLI runs on the SSH side | Start `adact serve pipe` in the GUI-side interactive session, and connect from the SSH-side CLI via Named Pipe |
| Want to stop the daemon | Run `adact daemon-stop` from the CLI in the same workspace (Named Pipe only) |
| Want to use an HTTP MCP client | Use `/mcp` on a running `adact serve http` instance |

## References

| Document | Description |
| --- | --- |
| [../spec/errors-and-output.md](../spec/errors-and-output.md) | Exit code and stderr conventions |
| [../development/troubleshooting.md](../development/troubleshooting.md) | Common recovery steps |
