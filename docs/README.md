# ADACT Documentation

ADACT (AI-driven Desktop Application CLI Tools) lets AI agents and humans inspect and control Windows desktop apps through the same `adact <subcommand>` CLI. It brings Playwright-style snapshot/ref interaction to Windows UI Automation (UIA), targeting desktop apps such as WPF, WinForms, UWP, and Win32 instead of browsers.

The current primary interface is the `adact <subcommand>` CLI, not direct MCP access. The CLI connects by default to the Named Pipe MCP daemon, and `--server` switches it to the HTTP MCP daemon. The daemon drives the target Windows app through UIA.

Start with the diagrams if you want the design story. See [architecture/overview.md](architecture/overview.md) for the big picture, [architecture/class-responsibilities.md](architecture/class-responsibilities.md) for class relationships, [architecture/command-flows.md](architecture/command-flows.md) for runtime flow, and [architecture/snapshot-pipeline.md](architecture/snapshot-pipeline.md) for the snapshot/ref pipeline.

## Sitemap

| Category | Document | Description |
| --- | --- | --- |
| Architecture | [architecture/overview.md](architecture/overview.md) | Big picture, component relationships, primary interface |
| Architecture | [architecture/runtime-modes.md](architecture/runtime-modes.md) | Differences between `adact <sub>` and `adact serve http` / `adact serve pipe` |
| Architecture | [architecture/components.md](architecture/components.md) | Responsibilities of each project and major class |
| Architecture | [architecture/class-responsibilities.md](architecture/class-responsibilities.md) | Layered class responsibilities, state, call targets, dependency direction |
| Architecture | [architecture/command-flows.md](architecture/command-flows.md) | End-to-end flow from CLI subcommand to MCP tool, stores, engine, and CLI output |
| Architecture | [architecture/snapshot-pipeline.md](architecture/snapshot-pipeline.md) | Raw JSON generation, ref registration, and CLI `.txt` snapshot conversion |
| Spec | [spec/cli.md](spec/cli.md) | CLI subcommands, shared flags, and output formats |
| Spec | [spec/mcp-tools.md](spec/mcp-tools.md) | MCP tool specifications (`adact_*`, `adact_daemon_stop`) |
| Spec | [spec/ref-ids.md](spec/ref-ids.md) | `windowRef`, `sessionId`, and `elementRef` formats and lifecycles |
| Spec | [spec/snapshot.md](spec/snapshot.md) | Responsibility split between Engine/MCP raw JSON and CLI `.txt` snapshots |
| Spec | [spec/errors-and-output.md](spec/errors-and-output.md) | Exit codes, stderr, stdout, and MCP error conventions |
| Development | [development/testing.md](development/testing.md) | Test structure, Layer traits, commands, and real-app E2E notes |
| Development | [development/building.md](development/building.md) | Shared MSBuild settings, CLI versioning, and Git-less build fallback |
| Development | [development/troubleshooting.md](development/troubleshooting.md) | Common failures and recovery steps |
| Roadmap | [roadmap/phase8-and-beyond.md](roadmap/phase8-and-beyond.md) | Remaining tasks and ideas for Phase 8+ |

## Reading guide

| Audience | First | Next |
| --- | --- | --- |
| Want to operate Windows apps with ADACT | [spec/cli.md](spec/cli.md) | [development/troubleshooting.md](development/troubleshooting.md) |
| Want to understand MCP integration and AI clients | [architecture/overview.md](architecture/overview.md) | [spec/mcp-tools.md](spec/mcp-tools.md) |
| Starting implementation work | [architecture/components.md](architecture/components.md) | [architecture/class-responsibilities.md](architecture/class-responsibilities.md), [architecture/command-flows.md](architecture/command-flows.md), [development/testing.md](development/testing.md) |
| Working on snapshot/ref behavior | [architecture/snapshot-pipeline.md](architecture/snapshot-pipeline.md) | [spec/ref-ids.md](spec/ref-ids.md), [spec/snapshot.md](spec/snapshot.md) |
| Planning the next phase | [roadmap/phase8-and-beyond.md](roadmap/phase8-and-beyond.md) | discussion notes |

## Relationship to discussion/

`discussion/` stores exploration notes, design decisions, and completion logs. `docs/` is the stable, current-implementation view extracted from those notes. When older discussion notes disagree with the current implementation, these docs favor the current implementation.
