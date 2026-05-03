# `adact daemon-stop`

Stop the local Named Pipe MCP daemon gracefully.

## Synopsis

```
adact daemon-stop
```

## Description

`daemon-stop` sends a `daemon_stop` tool request to the Named Pipe daemon,
requesting it to shut down gracefully.

- **HTTP mode is not supported**. If you pass `--server`, the command returns
  `LOCAL_ONLY` error immediately. HTTP servers must be stopped with Ctrl+C or
  task management tools.
- The command connects via Named Pipe (workspace path is resolved
  automatically).
- If the daemon stops before the response is received, the command still
  returns success (exit code 0) with output `stopped`.

## Options

None. `--server` is rejected with `LOCAL_ONLY` error.

## Output

On success:

```
stopped
```

## Examples

Stop the Named Pipe daemon:

```
> adact daemon-stop
stopped
```

Attempting with `--server` fails:

```
> adact daemon-stop --server http://127.0.0.1:41300/mcp
error LOCAL_ONLY
message daemon-stop is not supported for HTTP mode. Use Ctrl+C to stop the server.
hint For HTTP server, stop the process manually or use task management tools.
```

## Error recovery

- `LOCAL_ONLY` — `--server` was specified. HTTP mode does not support
  `daemon-stop`. Remove `--server` to use Named Pipe mode, or stop the HTTP
  server manually.
- `CONNECTION_FAILED` — could not connect to the Named Pipe daemon. Ensure
  `adact serve pipe` is running in the same workspace.

## See also

- `adact serve pipe` — Start the Named Pipe daemon
- `adact serve http` — HTTP daemon (does not support daemon-stop)
