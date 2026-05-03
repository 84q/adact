# `adact serve pipe`

Start the ADACT Named Pipe MCP daemon. This is the default transport mode for
CLI clients.

## Synopsis

```
adact serve pipe
```

## Description

`serve pipe` launches the Named Pipe transport daemon. It accepts MCP client
connections over a Windows Named Pipe and maintains session state in memory.

- The daemon must run on an **interactive Windows desktop** (not a service or
  SSH session). It checks `WinSta0` and `SessionId` at startup and exits with
  code `4` (`NO_INTERACTIVE_SESSION`) if the check fails.
- Sessions and window refs persist only while the daemon is running.
- Named Pipe mode supports `daemon-stop` for graceful shutdown.
- The Pipe name is automatically generated from the workspace path hash:
  `\\.\pipe\adact-<hash>-<session>`

## Options

None.

## Output

No stdout output on success. Daemon logs and startup messages are written to
stderr.

## Examples

Start the Named Pipe daemon:

```
> adact serve pipe
Named Pipe server started: \\.\pipe\adact-a1b2c3d4-MySession
```

## Error recovery

- `NO_INTERACTIVE_SESSION` — the daemon is not running on an interactive
  desktop. Launch it from a logged-on user session that owns the target GUI
  windows.
- `PIPE_ACCESS_DENIED` — the Pipe could not be created due to permissions.
  Check that the current user has rights to create Named Pipes.

## See also

- `adact serve http` — HTTP transport mode
- `adact daemon-stop` — Stop the Named Pipe daemon
