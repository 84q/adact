# Phase 8 and Beyond Roadmap

This document is the stable snapshot of [discussion note 019](../../discussion/019_Phase8%E4%BB%A5%E9%99%8D%E3%81%AE%E6%AE%8B%E3%82%BF%E3%82%B9%E3%82%AF%E6%95%B4%E7%90%86.md). It organizes the remaining tasks and candidate ideas at a maintainable level based on the current Phase 7 implementation.

## Current summary

| Phase | Status | Content |
| --- | --- | --- |
| Phase 5 | Done | Core CLI: `list-windows`, `attach`, `snapshot`, `click`, `fill`, and lifecycle commands (`detach`, `close-window`, `kill`, `daemon-stop`) |
| Phase 5 post-task | Done | Element ref stabilization; migrated to generation-free `s<sid>e<eid>` refs |
| Phase 6 | Done | Skill installation via `adact install --skills` |
| Phase 7 | Implemented; some acceptance work remains | CLI `.txt` snapshots, MCP raw JSON, and the CLI-side filter/formatter split |

The current single `adact.exe` / `src/Adact.Cli` build is Windows-targeted. `serve http` / `serve pipe` need the same interactive Windows session as the target GUI, and that boundary will remain even if a cross-platform CLI client is introduced later.

## Phase 7 follow-up items

| Task | Description | Notes |
| --- | --- | --- |
| Modal dialog follow-up | Detect modal dialogs and follow them naturally | The current design already injects modal nodes into snapshots |
| Screen-lock detection | Make locked or otherwise non-operable desktop states explicit | Separate from the startup interactive-session check |
| Better failure logs | Surface ref, element details, and exception data when click/fill/snapshot fails | Helps both humans and AI recover |
| Structured logging / `--verbose` | Make CLI and daemon logs easier to operate | `adact serve pipe --verbose` exists, but the whole story is not finished |
| Failure-time screenshots | Save screenshots on failure for diagnosis | Pairs naturally with the `screenshot` command |
| Snapshot tuning | Skip duplicate properties, add fields, truncate long values, etc. | Consider when real usage demands it |

## Follow-ups from Phases 5 and 6

| Task | Description | Source |
| --- | --- | --- |
| Authentication / TLS / CORS | Decide the protection model for remote daemon use | Phase 5 |
| Re-snapshot policy for `REF_NOT_FOUND` | Decide how much should be automatic vs. human-driven | Phase 5 / 6 |
| `.adact/config.json` expansion | Add more settings and separate per-user and repo-shared configuration | Phase 5 |
| PID reuse protection in `KillAsync` | Avoid killing the wrong process if PIDs are reused | Phase 5 |
| Recipes | Provide common operation templates such as Calculator and Notepad | Phase 6 |
| Skill coverage for new commands | Sync the `adact-cli` Skill and tests when commands change | Phase 6 |

### Phase 8-A: operation foundation

| Feature | Goal |
| --- | --- |
| `launch` | Start apps from the CLI and reduce manual pre-attach work |
| `wait-for-element` | Stabilize GUI flows with explicit waits for window/element/state |
| `keypress` | Handle key presses against a specific or active element |
| `type` | Handle incremental input and IME/keystroke-style validation |

### Phase 8-B: diagnostics and low-level operations

| Feature | Goal |
| --- | --- |
| `screenshot` | Add diagnostic attachments and a foundation for future Vision/OCR work |
| `hover` | Support tooltips and hover menus |
| `keyboard` | Support key down/up, shortcuts, and held keys |
| `mouse` | Provide an escape hatch for cases UIA cannot reach |

### Phase 8-C: add when needed

| Feature | Goal | Decision rule |
| --- | --- | --- |
| `select-option` | ComboBox/ListBox selection | Add when main apps need it |
| `get-value` | Explicitly read an element value | Add when snapshot is not enough |
| `evaluate` | App-specific escape hatch | Add only after carefully designing safety and API boundaries |

## Phase 9+ ideas

| Idea | Description |
| --- | --- |
| Dashboard | Visualize daemon/session/window/snapshot state |
| OCR / Vision | Combine image recognition or OCR with UIA for weak apps |
| Stable selector generation | Generate selectors that can be rerun, not just temporary refs |
| Codegen | Turn AI/human actions into test code |
| State persistence | Restore sessions/windows/settings after daemon restart |
| Recipes | Distribute common operation templates |
| Cross-platform CLI client | Keep the daemon on the Windows GUI session side and split out the GUI-independent CLI client so it can run on macOS/Linux terminals |
| Sample validation app | Provide a dedicated app for repeatable snapshot/wait/keyboard/mouse/modal validation |
| Standalone `adact` distribution path | Make `adact` available via installer, .NET tool, or PATH-based launch |
| FlaUI test code generation | Generate automated scenario tests from explored operations |

## Open questions

| Item | What to decide |
| --- | --- |
| Phase 8 scope | Whether to keep the 8-A / 8-B / 8-C split |
| `launch` | Target types, working directory, env, and post-launch attach behavior |
| `wait-for-element` | Whether to cover window, element, text, value, and disappearance |
| `keypress` / `type` / `keyboard` | How to split UIA pattern use from Win32 input injection |
| `mouse` | Coordinates, out-of-window behavior, DPI handling, and safety |
| `evaluate` | Whether to accept it as a general escape hatch |
| Authentication / TLS / CORS | When remote daemon support becomes a first-class concern |
| Recipes | Whether they belong in `adact-cli` or a separate Skill |
| Cross-platform CLI client | Scope, project split, and multi-targeting plan |
| Distribution | .NET tool, self-contained binary, installer, PATH setup, etc. |
| Sample app | Technology choice and validation scope |
| FlaUI test generation | Output format and boundary with AI exploration |
