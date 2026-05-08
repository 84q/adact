# Troubleshooting

ADACT depends on Windows UIA and daemon-process state. When something goes wrong, first confirm that the daemon is running in the same interactive session as the target GUI and that the CLI is connecting to the expected transport (Named Pipe by default, or `--server` for HTTP).

## `NO_INTERACTIVE_SESSION`

| Item | Description |
| --- | --- |
| Exit code | `4` |
| stderr | `error NO_INTERACTIVE_SESSION` |
| Where it happens | At `adact serve http` / `adact serve pipe` startup |
| Cause | The daemon was started from SSH, a service, or session 0 instead of an interactive desktop |

Recovery:

1. Sign in to the Windows logon session that shows the target GUI app.
2. Start the matching daemon mode from that session (`adact serve pipe` for the default CLI path, or `adact serve http --port <port>` when using `--server`).
3. Point CLI clients from SSH or other terminals to that daemon with `--server` or `.adact/config.json`.

## `OPERATION_BLOCKED`

| Item | Description |
| --- | --- |
| Exit code | Usually `1` |
| stderr | `error OPERATION_BLOCKED` |
| Typical cause | The screen is locked, a UAC prompt is open, or the target window is inactive/minimized |

Recovery:

1. Unlock the screen if needed.
2. Close or respond to any UAC prompt.
3. Make sure the target window is active and visible.

## Daemon connection failure

| Item | Description |
| --- | --- |
| Exit code | `3` |
| stderr | `error CONNECTION_FAILED` |
| Typical cause | The daemon is not running, the port is wrong, the URL does not end with `/mcp`, or a firewall/port-forwarding issue exists |

Check with:

```powershell
adact serve http --port 41300
adact list-windows --server http://127.0.0.1:41300/mcp
```

If you use `.adact/config.json`:

```json
{ "server": "http://127.0.0.1:41300/mcp" }
```

Connection resolution checks `--server`, then `.adact/config.json`, then the default Named Pipe endpoint. Once `.adact/` is found, the search stops.

## `REF_NOT_FOUND`

| Item | Description |
| --- | --- |
| Exit code | Usually `1` |
| Where it happens | `click` / `fill`, or MCP `adact_click` / `adact_fill` |
| Typical cause | A typo in the ref, a ref from another session, the element disappeared from the latest snapshot, or daemon restart cleared state |

Recovery:

1. Run `adact snapshot` again.
2. Use the new `[ref=s...e...]` from the fresh snapshot.
3. If the session itself is gone, start again from `adact list-windows` -> `adact attach ...`.

Current refs use `s<sid>e<eid>`. The old `s<sid>g<gen>e<eid>` form is legacy.

## `INVALID_WINDOW_REF` / `WINDOW_NOT_FOUND`

| Code | Cause | Fix |
| --- | --- | --- |
| `INVALID_WINDOW_REF` | `w<n>` is unregistered or retired | Re-run `list-windows` and use the latest `w<n>` |
| `WINDOW_NOT_FOUND` | HWND attach failed after resolving `w<n>` | Re-run `list-windows` and confirm the window still exists |

Example:

```powershell
adact list-windows
adact attach w1
```

`attach` accepts only the positional `w<n>` ref. Use `list-windows` to narrow down the target first.

## Snapshot is too large

| Situation | Fix |
| --- | --- |
| Want a smaller AI-facing snapshot | Use the default `--filter operable` |
| Want the full tree for debugging | Use `adact snapshot --filter raw` |
| Want a separate output folder | Use `--snapshot-dir <dir>` |
| Do not want a post-click/fill snapshot | Use `--no-snapshot` |

The current CLI snapshot format is `.txt`, which is usually smaller than the old JSON output.

## Missing elements in the snapshot

| Possibility | Fix |
| --- | --- |
| Filtered out by `operable` | Check with `adact snapshot --filter raw` |
| Element is offscreen | Show, expand, or scroll the window and snapshot again |
| Focus is on a modal dialog | Check the `[modal]` nodes in the snapshot |
| UIA does not expose the element | Verify the app's UIA support; future OCR/Vision may help |
| Looking at an old snapshot | Use the stdout `snapshot <path>` value to confirm the newest file |

## `daemon-stop` is `LOCAL_ONLY`

| Symptom | Cause | Fix |
| --- | --- | --- |
| `error LOCAL_ONLY` | You tried to stop a remote daemon, or called `adact_daemon_stop` in stdio mode | Run `adact daemon-stop` from the same host as the daemon |

`daemon-stop` is localhost-only for safety.

## Real-app tests are flaky

| Symptom | Fix |
| --- | --- |
| Calculator E2E collides with another run | Make sure no other test run is using Calculator |
| click/fill sometimes fail | Do not interact with the same desktop while tests are running |
| Notepad++ smoke skips or fails | Check installation, window title, and permissions |
| `list-windows` returns empty | Confirm the daemon is running in an interactive session |

## References

| Document | Description |
| --- | --- |
| [../architecture/runtime-modes.md](../architecture/runtime-modes.md) | Runtime modes and interactive-session constraints |
| [../spec/errors-and-output.md](../spec/errors-and-output.md) | Error code list |
| [../spec/ref-ids.md](../spec/ref-ids.md) | Ref invalidation rules |
| [../spec/snapshot.md](../spec/snapshot.md) | Snapshot filters and format |
