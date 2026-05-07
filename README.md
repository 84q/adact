# ADACT

**ADACT (AI-driven Desktop Application CLI Tools) is an AI-agent-centric CLI for automating Windows desktop applications through structured snapshots and stable element refs.**

It brings a Playwright-like snapshot/ref workflow to Windows UI Automation, so AI agents and developers can inspect desktop UI, attach to windows, and perform repeatable actions from the command line.

## What it can do

- Discover top-level Windows application windows with `list-windows`
- Attach to a window and keep a session for follow-up commands
- Capture text snapshots of the UI tree for agent-friendly inspection
- Act on stable element refs with commands such as `click`, `fill`, `press`, `select`, and `screenshot`
- Run as a local daemon over Named Pipe or HTTP, depending on your workflow
- Install AI coding client skill files with `adact install --skills ...`

## Installation

Download Windows builds from this repository's [GitHub Releases](https://github.com/84q/adact/releases) page.

- **framework-dependent ZIP**: smaller download; requires a compatible .NET 10 runtime on the machine.
- **self-contained ZIP**: larger download; includes the runtime so you can run ADACT on supported Windows without a separate .NET install.

Source build is still supported as a fallback and development path.

### Source build requirements

- Windows
- .NET 10 SDK

### Build from source

```powershell
git clone <this-repository-url>
cd adact
dotnet build adact.sln
```

The main CLI entry point is in `src/Adact.Cli/`. When using ADACT from a fresh source checkout, run commands with:

```powershell
dotnet run --project src/Adact.Cli -- <subcommand>
```

Examples:

```powershell
dotnet run --project src/Adact.Cli -- list-windows
dotnet run --project src/Adact.Cli -- serve pipe
```

For more project docs, see [docs/README.md](docs/README.md).

## Quick start

The examples below use `adact.exe` from an extracted release ZIP. If you are running from a source checkout instead, replace `adact` with `dotnet run --project src/Adact.Cli --`.

Start the daemon in one terminal:

```powershell
# Named Pipe mode
.\adact.exe serve pipe

# or HTTP mode
.\adact.exe serve http --port 41300
```

Then use the CLI from another terminal:

```powershell
# 1. List top-level windows
.\adact.exe list-windows

# 2. Attach to a window from the list (example: w1)
.\adact.exe attach w1

# 3. Capture a UI snapshot for inspection
.\adact.exe snapshot

# 4. Perform an action on an element ref from the snapshot
.\adact.exe click s1e12
.\adact.exe fill s1e20 "hello from adact"
.\adact.exe press "Ctrl+S"
```

If you use HTTP mode, add `--server http://127.0.0.1:41300/mcp` to client commands.

To help AI coding clients discover the ADACT workflow, install the bundled skill files:

```powershell
.\adact.exe install --skills claude
# or: copilot / codex
```

For the full CLI reference, see [docs/spec/cli.md](docs/spec/cli.md).

## Architecture overview

ADACT has a simple layered flow:

```text
AI agent / Human
  -> adact CLI
  -> adact serve daemon
  -> UI Automation engine
  -> Windows desktop app
```

The CLI is the main user-facing interface. The daemon keeps session and element-ref state in memory, and the engine performs the actual Windows UI Automation work.

For more detail, see [docs/architecture/overview.md](docs/architecture/overview.md).

## Documentation

- [docs/README.md](docs/README.md) — documentation index
- [docs/spec/cli.md](docs/spec/cli.md) — CLI commands and output formats
- [docs/architecture/overview.md](docs/architecture/overview.md) — system overview
- [docs/development/testing.md](docs/development/testing.md) — test strategy and commands
- [docs/roadmap/phase8-and-beyond.md](docs/roadmap/phase8-and-beyond.md) — roadmap and current maturity

## Contributing

Issues and pull requests are welcome.

Basic development commands:

```powershell
dotnet build adact.sln
dotnet test --filter "Layer=Unit|Layer=Integration"
```

Please keep changes focused and use the docs in `docs/` as the source of deeper implementation and testing guidance.

## License

This repository is licensed under the [MIT License](LICENSE).
