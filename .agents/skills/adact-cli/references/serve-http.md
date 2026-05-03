# `adact serve http`

Start the ADACT HTTP MCP daemon. The daemon binds to `127.0.0.1` and exposes
MCP tools at `/mcp`.

## Synopsis

```
adact serve http [--port <0-65535>]
```

## Description

`serve http` launches the HTTP transport daemon. It accepts MCP client
connections over HTTP and maintains session state in memory.

- The daemon must run on an **interactive Windows desktop** (not a service or
  SSH session). It checks `WinSta0` and `SessionId` at startup and exits with
  code `4` (`NO_INTERACTIVE_SESSION`) if the check fails.
- Sessions and window refs persist only while the daemon is running.
- HTTP mode does **not** support `daemon-stop`. Use Ctrl+C or task management
  tools to stop the server.

## Options

- `--port <port>` — TCP port to bind (default: 41300). Port 0 lets the OS
  assign an ephemeral port.

## Output

No stdout output on success. Daemon logs and startup messages are written to
stderr.

## Examples

Start on the default port:

```
> adact serve http
Server started at http://127.0.0.1:41300/mcp
```

Start on a custom port:

```
> adact serve http --port 8080
Server started at http://127.0.0.1:8080/mcp
```

## Error recovery

- `NO_INTERACTIVE_SESSION` — the daemon is not running on an interactive
  desktop. Launch it from a logged-on user session that owns the target GUI
  windows.
- `ADDRESS_IN_USE` — the port is already in use. Choose a different port or
  stop the conflicting process.

## See also

- `adact serve pipe` — Named Pipe transport mode (default)
- `adact daemon-stop` — Stop the Named Pipe daemon (HTTP not supported)
