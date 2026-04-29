# `adact list-apps`

List every top-level window currently visible on the Windows desktop.

Use it before `attach` to discover the target window's `processName`,
`processId`, `className` or `windowTitle` so you can match it unambiguously.

## Synopsis

```
adact list-apps [--server <url>]
```

No required arguments. `--server` selects the ADACT daemon endpoint; omit it
to use `./.adact/config.json` or the default `http://127.0.0.1:41300/mcp`.

## Output

Tab-separated values written to stdout. The first row is the header:

```
windowRef	sessionId	processName	processId	className	windowTitle
```

- `windowRef` — `w<n>`, can be passed back to `attach <windowRef>`.
- `sessionId` — non-empty only if a session is already attached to that window.
- The remaining columns are the strict-equal matching keys understood by
  `attach`.

## Examples

Find Notepad:

```
> adact list-apps
windowRef	sessionId	processName	processId	className	windowTitle
w1		Notepad	1234	Notepad	Untitled - Notepad
w2		explorer	5678	CabinetWClass	Documents
```

Then attach by `windowRef`:

```
adact attach w1
```

…or by exact criteria from one of the columns:

```
adact attach --process-name Notepad --title "Untitled - Notepad"
```

## Error recovery

- `CONNECTION_FAILED` — the ADACT daemon is not running. Start it with
  `adact serve` (or pass `--server <url>` to point at an existing one).
- An empty result (only the header) means UIA returned no top-level windows,
  which is rare on a logged-in desktop. Confirm the target window is actually
  visible and not minimized to the system tray.
