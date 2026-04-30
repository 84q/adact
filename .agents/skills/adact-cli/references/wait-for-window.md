# `adact wait-for-window`

Wait until a top-level window matching the given conditions appears on the
desktop. Useful for synchronizing with application launches or new dialogs
**before** running `attach`.

`wait-for-window` does not attach to the discovered window and does not
capture a snapshot. It polls the desktop every ~100 ms until a match is
found or the timeout expires.

## Synopsis

```
adact wait-for-window [--title <regex>] [--class-name <regex>] [--process-name <regex>]
                      [--exe <regex>] [--timeout <ms>] [--server <url>]
```

At least one of `--title`, `--class-name`, `--process-name`, `--exe` must
be supplied.

| Flag             | Purpose                                                                              |
| ---------------- | ------------------------------------------------------------------------------------ |
| `--title`        | Window title regex (case-insensitive).                                               |
| `--class-name`   | Win32 ClassName regex (case-insensitive).                                            |
| `--process-name` | Process name regex (case-insensitive, no extension, e.g. `notepad`, `CalculatorApp`). |
| `--exe`          | Process executable full-path regex (case-insensitive).                               |
| `--timeout`      | Polling timeout in milliseconds. Defaults to `5000`.                                 |
| `--server`       | Daemon endpoint URL.                                                                 |

All conditions are AND-combined. The first matching window is returned.

## Output

A single JSON line on stdout describing the matched window:

```json
{"processId":12345,"processName":"notepad","windowTitle":"Untitled - Notepad","controlType":"Window","className":"Notepad","nativeWindowHandle":1247432}
```

The fields mirror the entries returned by `adact list-apps`.

## Examples

Wait for Notepad to appear (by process name) after launching it:

```
adact launch notepad
adact wait-for-window --process-name notepad --timeout 10000
```

Wait for a confirmation dialog by title:

```
adact wait-for-window --title "^Confirm "
```

## Typical follow-up

After this command succeeds, run `adact list-apps` to obtain the `windowRef`
and then `adact attach <windowRef>`. `wait-for-window` itself is intentionally
attach-free so it can be used to synchronize on side-band windows that you
do not want to make active.

## Error recovery

- `INVALID_ARGUMENT` — no condition was provided, or `--timeout` was not
  positive.
- `WAIT_TIMEOUT` — no window matched within `--timeout`. Verify the regex
  is anchored correctly (regex is unanchored substring match by default)
  and increase the timeout if the launch is slow.
