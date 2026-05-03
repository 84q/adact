# `adact launch`

Start a Windows process and print its `pid` so you can drive it with the rest
of the ADACT toolchain. `launch` only starts the process; it does **not**
attach to it. Run `adact list-apps` followed by `adact attach` after the
target window appears.

## Synopsis

```
adact launch <executable> [--cwd <dir>] [--env KEY=VALUE]... [-- <arg>...]
            [--server <url>]
```

- `<executable>` — full path, PATH-resolved name (e.g. `notepad.exe`), or
  `shell:AppsFolder\<AUMID>` for a UWP / Microsoft Store app.
- `--cwd <dir>` — working directory for the new process. Not allowed when the
  target is UWP.
- `--env KEY=VALUE` — merge an environment variable. Repeat for multiple.
  Not allowed when the target is UWP.
- `-- <arg>...` — everything after `--` is passed as raw arguments to the
  target executable; ADACT does not interpret it.
- `--server <url>` — HTTP daemon endpoint; omit to use Named Pipe (default).

## Output

A single JSON object on stdout:

```json
{"pid":1234,"processName":"notepad","executablePath":"C:\\Windows\\System32\\notepad.exe"}
```

- `pid` — process ID. Use it with `adact list-apps` / `adact attach` to find
  and attach to the resulting window.
- `processName` — process basename without extension (e.g. `notepad`).
- `executablePath` — resolved full path. For Win32 / .NET targets this is
  read via `Process.MainModule.FileName` and may be `null` when the daemon
  cannot open the child process module (typically a permission or 32/64-bit
  bitness mismatch). For UWP launches the AUMID is always returned.

`launch` does not wait for the window to appear. Poll with `list-apps` if the
target needs time to spin up.

## Examples

Start Notepad with no arguments:

```
adact launch notepad.exe
```

Pass arguments through `--`:

```
adact launch notepad.exe -- C:\path\to\file.txt
```

Set the working directory and override an environment variable:

```
adact launch python.exe --cwd C:\projects\demo --env PYTHONUNBUFFERED=1 -- script.py
```

Start the Windows Calculator (UWP):

```
adact launch shell:AppsFolder\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App
```

## Error recovery

- `LAUNCH_FAILED` — the executable was not found or `Process.Start` failed.
  Verify the path / PATH, file permissions, and that the daemon has the same
  privilege level you expect.
- `INVALID_ARGUMENT` — typically `--cwd` or `--env` was supplied together
  with a `shell:AppsFolder\` target, or `--env` was missing the `=` separator.
  Drop the unsupported flags or rewrite the entry as `KEY=VALUE`.
- `CONNECTION_FAILED` — the ADACT daemon is not running. Start it with
  `adact serve pipe` (or `adact serve http` for HTTP mode).
