# Snapshot Pipeline

This document describes how ADACT snapshots move from Engine raw JSON to CLI `.txt` snapshots. For the field schema, see [../spec/snapshot.md](../spec/snapshot.md). For ref format, see [../spec/ref-ids.md](../spec/ref-ids.md).

## Boundary model

| Boundary | Output | Owner | Purpose |
| --- | --- | --- | --- |
| Engine / MCP | Raw snapshot JSON | `WindowSession.SnapshotAsync()`, `SnapshotBuilder.Build()`, `adact_snapshot` | Return UIA data with minimal loss |
| CLI | `.txt` snapshot | `WriteSnapshotResultAsync()`, parser, filter, formatter, writer | Make the snapshot easier for humans and AI to read |

After Phase 7, filtering is handled only on the CLI side. The Engine and MCP always return raw JSON.

## Data flow

| Data | Source | Through | Used by |
| --- | --- | --- | --- |
| `windowRef` (`w<n>`) | `WindowRefStore.SyncOrAssign()` | `list-windows`, `attach` | Window attach and idempotent attach |
| `sessionId` (`s<n>`) | `UiaEngine` / `SessionStore.Register()` | MCP response, CLI stdout, element-ref prefix | Session targeting and lifecycle |
| `elementRef` (`s<sid>e<eid>`) | `RefRegistry.Register()` | Raw JSON and CLI `.txt` snapshot | `click` / `fill` targeting |
| Raw JSON | `SnapshotBuilder.Build()` | `adact_snapshot` response | CLI parser / filter / formatter |
| `.txt` snapshot | Text formatter and file writer | `.adact/` or `--snapshot-dir` | Human- and AI-readable output |

## Engine side: `WindowSession.SnapshotAsync`

1. `WindowsTools.SnapshotAsync()` gets the target `WindowSession`.
2. The session takes the shared UIA gate.
3. Modal dialogs are detected and added as modal sibling nodes when needed.
4. `SnapshotBuildInput` is assembled from the root element, modal siblings, options, and metadata.
5. `SnapshotBuilder` walks the tree and builds raw JSON.
6. The result contains the raw JSON, `sessionId`, metadata, and timestamp.
7. Unexpected exceptions are wrapped and mapped to `SNAPSHOT_FAILED`.

## Engine side: `SnapshotBuilder.Build`

`SnapshotBuilder` converts the `IElement` tree to raw JSON with a DFS walk.

| Step | Description |
| --- | --- |
| Start | Clear the current-snapshot element map |
| Depth guard | Use `MaxDepth` when provided; otherwise use the default |
| Node read | Read role, name, automation id, class name, state, value, help text, bounds, focus, and children |
| Ref assignment | Allocate `s<sid>e<eid>` via `RefRegistry` |
| Modal insertion | Add modal siblings under the root and mark them as modal dialogs |
| Metadata | Fill `_meta` with options, timestamps, session info, and modal summary |
| Output | Return `{"_meta": ..., "tree": ...}` and the session id |

Raw JSON is not filtered in the Engine.

## `RefRegistry`

`RefRegistry` manages session-scoped element refs.

| State | Purpose |
| --- | --- |
| Stable-key map | Reuse the same `eid` for the same element across snapshots |
| Current-snapshot map | Resolve refs only for elements visible in the most recent snapshot |
| Next id | Allocate monotonically increasing `eid` values |

1. `BeginSnapshot()` clears only the current-snapshot map.
2. `Register()` prefers `RuntimeId` when available.
3. If no `RuntimeId` exists, it falls back to positional order.
4. Existing stable keys reuse the same `eid`.
5. Current-snapshot refs are recorded for the elements seen in this snapshot.
6. `Resolve()` rejects malformed refs, session mismatches, and refs that are not in the current snapshot.

## MCP `adact_snapshot`

1. `WindowsTools.SnapshotAsync()` acquires the tool-level lock.
2. If `sessionId` is omitted, the active session is used.
3. If `sessionId` is provided, it is resolved through `SessionStore`.
4. Raw JSON is placed into the MCP tool result.
5. The same raw JSON is also exposed as structured content.

The MCP tool returns raw JSON only; it does not know about CLI filtering, formatting, or file locations.

## CLI `WriteSnapshotResultAsync`

`WriteSnapshotResultAsync()` is shared by `snapshot`, the attach auto-snapshot, and other auto-snapshot-enabled commands.

1. Default the filter to `operable`.
2. Call `adact_snapshot` with `sessionId` when needed.
3. Convert MCP errors to CLI output.
4. Read the resolved session id from the response meta when available.
5. Parse the raw JSON into CLI DTOs.
6. Apply the selected filter.
7. Format the tree into `.txt`.
8. Write the snapshot file and return the relative path.
9. Print `sessionId` and the snapshot path.

## CLI parser / filter / formatter / writer

| Type | Input | Output | Role |
| --- | --- | --- | --- |
| `SnapshotJsonParser` | Engine raw JSON | Snapshot DTOs | Separate raw JSON parsing from CLI formatting |
| `SnapshotTreeFilter` | Parsed tree and filter name | Filtered tree | Keep the tree easy to operate on |
| `SnapshotTextFormatter` | Metadata and filtered tree | Frontmatter plus `.txt` body | Produce readable Playwright-style output |
| `SnapshotFileWriter` | Snapshot text and target dir | Relative snapshot path | Save artifacts under `.adact/` or a custom directory |

`raw` keeps the tree structure intact. `operable` keeps meaningful controls, flattens anonymous containers, and drops offscreen subtrees.

## Auto-snapshot commands

State-changing commands such as `click` and `fill` auto-capture a snapshot after success.

| Category | Commands | Auto-snapshot |
| --- | --- | --- |
| State-changing | `click`, `fill`, `doubleclick`, `hover`, `type`, `keypress`, `check`, `uncheck`, `select`, `mousewheel`, `resize-window`, `minimize-window`, `maximize-window`, `restore-window` | Yes, unless `--no-snapshot` is set |
| Low-level helpers | `mousemove`, `mousedown`, `mouseup`, `keydown`, `keyup`, `focus`, `scroll` | No |
| Read/sync commands | `inspect`, `screenshot`, `wait-for-element`, `wait-for-window`, `launch` | No |

## Ref lifetime and failure points

| Ref | Owner | Valid scope | Typical failure |
| --- | --- | --- | --- |
| `windowRef` | `WindowRefStore` | Daemon process | `INVALID_WINDOW_REF` |
| `sessionId` | `SessionStore` | From attach until detach/close/kill/daemon-stop | `INVALID_ARGUMENT` or `NO_ACTIVE_SESSION` |
| `elementRef` | `WindowSession.RefRegistry` | Current session snapshot | `REF_NOT_FOUND` |

## Related documents

| Document | Description |
| --- | --- |
| [command-flows.md](command-flows.md) | Subcommand flow |
| [class-responsibilities.md](class-responsibilities.md) | Responsibility overview |
| [../spec/snapshot.md](../spec/snapshot.md) | Snapshot field schema |
| [../spec/ref-ids.md](../spec/ref-ids.md) | Ref format |
