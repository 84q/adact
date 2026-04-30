# `adact wait-for`

Wait until an element reaches a target state. Useful for synchronizing with
asynchronous UI changes (e.g. a button becoming enabled, a dialog appearing,
a spinner disappearing) before the next action.

`wait-for` does not capture a snapshot. Internally it polls a fresh UIA
snapshot every ~100 ms until the condition is satisfied or the timeout
expires.

## Synopsis

```
adact wait-for --ref <ref> [--state <state>] [--timeout <ms>] [--server <url>]

adact wait-for [--name <text>] [--control-type <type>] [--automation-id <id>] [--class-name <cn>]
               [--state <state>] [--timeout <ms>] [--sid <session>] [--server <url>]
```

`--ref` and the search conditions (`--name`, `--control-type`,
`--automation-id`, `--class-name`) are **mutually exclusive**. Specify
exactly one mode.

| Flag                 | Purpose                                                                                                         |
| -------------------- | --------------------------------------------------------------------------------------------------------------- |
| `--ref`              | Wait on an existing element ref (e.g. `s1e7`). Use this when you have a ref captured from an earlier snapshot.  |
| `--name`             | Search condition: UIA Name (case-insensitive exact match).                                                      |
| `--control-type`     | Search condition: UIA ControlType (e.g. `Button`).                                                              |
| `--automation-id`    | Search condition: AutomationId.                                                                                 |
| `--class-name`       | Search condition: ClassName.                                                                                    |
| `--state`            | Target state. One of `attached`, `detached`, `visible`, `hidden`, `enabled`, `disabled`. Defaults to `visible`. |
| `--timeout`          | Polling timeout in milliseconds. Defaults to `5000`.                                                            |
| `--sid`              | Target session id (only used in search-condition mode). Defaults to the active session.                         |
| `--server`           | Daemon endpoint URL.                                                                                            |

State semantics (Playwright-aligned):

- `attached` — the element exists in the UIA tree.
- `detached` — the element no longer exists in the UIA tree.
- `visible` — the element exists and `IsOffscreen == false`.
- `hidden` — the element exists and `IsOffscreen == true`.
- `enabled` — the element exists and `IsEnabled == true`.
- `disabled` — the element exists and `IsEnabled == false`.

`detached` is only valid in ref mode. Search-condition mode requires the
element to be discovered, so `detached` is rejected with `INVALID_ARGUMENT`.

## Output

A single JSON line on stdout:

```json
{"ref":"s1e7","state":"visible"}
```

In ref mode the `ref` field equals the input ref. In search-condition mode
it is the ref of the first matching element discovered by polling.

## Examples

Wait for a "Save" button to become enabled (search-condition mode):

```
adact wait-for --name "Save" --control-type Button --state enabled
```

Wait until a previously captured ref disappears (ref mode):

```
adact wait-for --ref s1e23 --state detached --timeout 10000
```

## Error recovery

- `INVALID_ARGUMENT` — `--ref` was combined with search conditions, or
  neither was supplied, or `--timeout`/`--state` was invalid, or `detached`
  was used in search-condition mode.
- `INVALID_REF_FORMAT` — `--ref` did not match `s<sid>e<eid>`.
- `REF_NOT_FOUND` — ref-mode wait was given a ref whose session is unknown.
  Re-attach and re-snapshot to obtain a fresh ref.
- `NO_ACTIVE_SESSION` — search-condition mode was used without an active
  session. Run `adact attach` first or pass `--sid`.
- `WAIT_TIMEOUT` — the target state was not observed within `--timeout`.
  Increase the timeout, verify the application reaches the expected state,
  or relax the search conditions.
