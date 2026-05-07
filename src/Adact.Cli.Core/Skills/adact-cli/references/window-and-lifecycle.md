# Window and lifecycle commands

Use this for: `resize-window`, `minimize-window`, `maximize-window`, `restore-window`, `detach`, `close-window`, `kill`.

## Window state/size

- `adact resize-window --width <w> --height <h>`: resize attached window. Either option may be omitted (current value is kept).
- `adact minimize-window`: minimize attached window.
- `adact maximize-window`: maximize attached window.
- `adact restore-window`: restore to normal state.

## Session/process lifecycle

- `adact detach`: detach session only; target window/process keeps running.
- `adact close-window`: close attached window via `WindowPattern.Close`/`WM_CLOSE`; success auto-detaches session.
- `adact kill`: terminate attached process; success auto-detaches session.
## Recovery hints

- `NO_ACTIVE_SESSION`: attach first (`adact attach ...`) or pass explicit session option if supported.
- `CLOSE_FAILED`: window rejected close; try app-specific save/confirm flow, then retry or use `kill`.
