# Window and lifecycle commands

Use this for: `resize`, `minimize`, `maximize`, `restore`, `detach`, `close`, `kill`, `close-all`.

## Window state/size

- `adact resize --width <w> --height <h>`: resize attached window.
- `adact minimize`: minimize attached window.
- `adact maximize`: maximize attached window.
- `adact restore`: restore to normal state.

## Session/process lifecycle

- `adact detach`: detach session only; target window/process keeps running.
- `adact close`: close attached window via `WindowPattern.Close`/`WM_CLOSE`; success auto-detaches session.
- `adact kill`: terminate attached process; success auto-detaches session.
- `adact close-all`: attempt close for all attached sessions and return per-session results.

## Recovery hints

- `NO_ACTIVE_SESSION`: attach first (`adact attach ...`) or pass explicit session option if supported.
- `CLOSE_FAILED`: window rejected close; try app-specific save/confirm flow, then retry or use `kill`.
