# Testing

ADACT tests are classified with xUnit and Layer traits. The detailed strategy is documented in [../../.github/skills/testing-strategy/SKILL.md](../../.github/skills/testing-strategy/SKILL.md). This document is the stable snapshot that matches the current `tests/Adact.*.Tests/` layout.

## Test projects

| Project | Target | Main layers |
| --- | --- | --- |
| `tests/Adact.Engine.Tests/` | UIA Engine, SnapshotBuilder, RefRegistry, exceptions, real-app smoke | `Unit`, `Integration`, `IntegrationUia`, `Smoke` |
| `tests/Adact.Cli.Tests/` | CLI commands, connection, output, snapshot pipeline, Skill install, CLI E2E | `Unit`, `Integration`, `Smoke`, `E2E` |
| `tests/Adact.Mcp.Common.Tests/` | `WindowsTools`, `WindowRefStore`, lifecycle tools | `Unit` |
| `tests/Adact.Mcp.Http.Tests/` | HTTP daemon smoke / Calculator E2E | `Smoke`, `E2E` |

## Layer traits

| Layer | Purpose | Environment dependency | Example |
| --- | --- | --- | --- |
| `Unit` | Pure logic, DTOs, formatter, validation, store | None | `RefRegistryTests`, `SnapshotTextFormatterTests`, `WindowRefStoreTests` |
| `Integration` | Component integration with `FakeElement` | Low | `SnapshotBuilderTests`, `InstallCommandIntegrationTests` |
| `IntegrationUia` | Direct FlaUI/UIA integration | Windows + real app | `CalculatorSnapshotTests` |
| `Smoke` | Lightweight end-to-end checks for CLI/Engine/daemon | Windows + app or daemon | `AdactCliSmokeTests`, `CalculatorSmokeTests` |
| `E2E` | Full MCP/CLI/daemon/app flow | High | `CalculatorCliE2ETests`, `CalculatorHttpE2ETests`, `WindowsToolsE2ETests` |

Use `[Trait("Layer", "Unit")]` and similar annotations.

## Real-app E2E notes

| Item | Note |
| --- | --- |
| Interactive session | UIA tests and `adact serve http` / `adact serve pipe` must run in the same interactive Windows session as the target GUI |
| Calculator | Multiple assemblies share it, so the tests are serialized with a named semaphore |
| Notepad++ | A Win32 smoke target; environment and installation differences matter |
| UIA focus | Human interaction on the same desktop can make tests flaky |
| Cleanup | Close or kill any apps started by a test |
| CI | `Unit` and `Integration` are CI-friendly; real-app layers are Windows-session scoped |

## UIA test serialization

`Adact.Engine.Tests` serializes `IntegrationUia` and `Smoke` with `[Collection("UiaSerial")]`. `Unit` and `Integration` can run in parallel.

## Basic commands

```powershell
dotnet build adact.sln
dotnet test --filter Layer=Unit
dotnet test --filter "Layer=Unit|Layer=Integration"
dotnet test --filter Layer=IntegrationUia
dotnet test --filter Layer=Smoke
dotnet test --filter Layer=E2E
dotnet test
```

If the filter contains `|`, quote it in PowerShell.

### Running L3+ from SSH or another non-interactive session

When running `IntegrationUia`, `Smoke`, or `E2E` from SSH or another non-interactive session, the app and UIA work must still run in an interactive Windows session.

Set `ADACT_SERVER_URL` to point the test process at an external daemon, then start the daemon on the interactive desktop session first.

```powershell
$env:ADACT_SERVER_URL = "http://127.0.0.1:41300/mcp"
dotnet test tests/Adact.Cli.Tests/Adact.Cli.Tests.csproj --filter "Layer=Smoke|Layer=E2E"
dotnet test tests/Adact.Mcp.Http.Tests/Adact.Mcp.Http.Tests.csproj --filter "Layer=Smoke|Layer=E2E"
```

When `ADACT_SERVER_URL` is set, those test projects reuse the external URL instead of starting their own daemon.

## Coverage collection

Layer-based coverage can be collected with the helper script or the manual commands in the original workflow. TestResults is ignored by git.

## When to add tests

| Change | Tests to add or update |
| --- | --- |
| Validation / formatter / parser | `Unit` |
| Engine raw JSON tree building | `Integration` with `FakeElement` |
| CLI parser / filter / formatter | `Unit` |
| Real UIA behavior | Minimal `IntegrationUia` or `Smoke` |
| CLI command addition | CLI `Unit`, plus `Smoke` / `E2E` if needed |
| MCP tool addition | `Adact.Mcp.Common.Tests` `Unit` and transport-specific E2E |
| CLI/MCP subcommand rename or addition | Update the Skill synchronization tests as well |

## References

| Document | Description |
| --- | --- |
| [../../.github/skills/testing-strategy/SKILL.md](../../.github/skills/testing-strategy/SKILL.md) | Full ADACT testing strategy |
| [../architecture/components.md](../architecture/components.md) | Production and test project mapping |
| Phase 5 testing status | Stable snapshot of the Phase 5 test state |
| Phase 7 testing status | Stable snapshot of the Phase 7 test state |
