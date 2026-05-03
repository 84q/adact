# Session bootstrap commands

Use this for: `list-apps`, `attach`, `launch`, `wait-for-window`.

## `adact list-apps`

- Purpose: list top-level windows currently visible on the interactive desktop.
- Typical use: pick a `windowRef` (`w<n>`) before `attach`.

## `adact attach <windowRef>`

- Purpose: attach to one top-level window and start/activate a session.
- Output: `sessionId`, `windowRef`, and `snapshot` path.
- Note: attach captures a snapshot automatically.

## `adact launch <target>`

- Purpose: start a process (Win32/.NET executable or UWP via `shell:AppsFolder\...`).
- Typical flow: `launch` -> `wait-for-window` -> `list-apps`/`attach`.

## `adact wait-for-window`

- Purpose: wait for a top-level window match by title/process/class criteria.
- Scope: does not attach by itself; use it as synchronization before `attach`.

## Common startup flow

```bash
adact launch notepad
adact wait-for-window --title "Notepad" --timeout 10000
adact list-apps
adact attach w1
```
