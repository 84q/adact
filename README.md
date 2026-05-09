# ADACT

**ADACT (AI-driven Desktop Application CLI Tools) is an AI-agent-centric CLI for automating Windows desktop applications through structured snapshots and stable element refs.**

It brings a Playwright-like snapshot/ref workflow to Windows UI Automation, so AI agents and developers can inspect desktop UI, attach to windows, and perform repeatable actions from the command line.

## Platform support

- **Windows 11 automation host:** x64 / arm64
- **Linux client:** x64
- **macOS client:** arm64
- **Remote use from macOS / Linux:** supported via a cross-platform CLI client that connects over HTTP to an ADACT daemon running on Windows

ADACT runs its UI Automation engine on Windows. You can use it either directly on a Windows machine or from macOS / Linux by connecting a client to a Windows host running the ADACT daemon.

## What it can do

- Discover top-level Windows application windows with `list-windows`
- Attach to a window and keep a session for follow-up commands
- Capture text snapshots of the UI tree for agent-friendly inspection
- Act on stable element refs with commands such as `click`, `fill`, `keypress`, `select`, and `screenshot`
- Run as a local daemon over Named Pipe or HTTP, depending on your workflow
- Install AI coding client skill files with `adact install --skills ...`

## Installation

Download a suitable build from this repository's [GitHub Releases](https://github.com/84q/adact/releases) page.

- **Windows 11 (`win-x64`, `win-arm64`)**: use a Windows build when you want to run the daemon on the desktop you are automating. This is also the simplest way to use ADACT locally on the same machine.
- **Linux (`linux-x64`)**: use the Linux client build when you want to connect remotely to a Windows machine running the ADACT daemon over HTTP.
- **macOS (`osx-arm64`)**: use the macOS client build when you want to connect remotely to a Windows machine running the ADACT daemon over HTTP.

Choose the package that matches your OS and architecture, extract it, and either add the extracted directory to your `PATH` or invoke the binary by relative path from that directory.

Source build is still supported as a fallback and development path.

### Source build requirements

- .NET 10 SDK
- Windows 11 when you need to run the UI Automation daemon / host locally
- macOS or Linux are supported for building and running the remote CLI client

### Build from source

**Local Windows host build**

Use this when you want to run the daemon and UI Automation host on Windows:

```powershell
git clone <this-repository-url>
cd adact
dotnet build adact.sln
```

**Remote client build (macOS / Linux)**

Use this when you only need the cross-platform CLI client that connects to a Windows host over HTTP:

```powershell
git clone <this-repository-url>
cd adact
dotnet build src/Adact.Cli.Client
```

The Windows automation host entry point is in `src/Adact.Cli/`. The cross-platform remote client entry point is in `src/Adact.Cli.Client/`.

For more project docs, see [docs/README.md](docs/README.md).

## Quick start

ADACT supports two common workflows:

1. **Local Windows automation**: run both the daemon and the CLI on a Windows 11 machine.
2. **Remote automation from macOS / Linux**: run the daemon on Windows 11, then connect to it from another machine over HTTP.

In the examples below, `adact` assumes the binary is already available in your shell, typically because you added the extracted release directory to your `PATH`. If not, invoke it by relative or absolute path instead.

### Local Windows workflow

If you are running from a fresh source checkout on Windows instead of an extracted release package, use:

```powershell
dotnet run --project src/Adact.Cli -- <subcommand>
```

Examples:

```powershell
dotnet run --project src/Adact.Cli -- list-windows
dotnet run --project src/Adact.Cli -- serve pipe
```

Start the daemon in one terminal:

```powershell
# Named Pipe mode
adact serve pipe

# or HTTP mode
adact serve http --port 41300
```

Then use the CLI from another terminal:

```powershell
# 1. List top-level windows
adact list-windows

# 2. Attach to a window from the list (example: w1)
adact attach w1

# 3. Capture a UI snapshot for inspection
adact snapshot

# 4. Perform an action on an element ref from the snapshot
adact click s1e12
adact fill s1e20 "hello from adact"
adact keypress "Ctrl+S"
```

If you use HTTP mode, add `--server http://127.0.0.1:41300/mcp` to client commands.

### Remote client workflow from macOS / Linux

If you are running from a fresh source checkout on the Windows host, start the daemon with:

```powershell
dotnet run --project src/Adact.Cli -- serve http --host 0.0.0.0 --port 41300
```

If you are using an extracted release package on the Windows host, run:

```powershell
adact serve http --host 0.0.0.0 --port 41300
```

`adact serve http` binds to localhost by default. For remote clients, bind it to an address the other machine can reach, such as `0.0.0.0` or the host's LAN IP.

Then, from macOS or Linux, connect to that Windows host over HTTP. If you are running from a source checkout on the client machine, use `dotnet run --project src/Adact.Cli.Client -- ...` instead of `adact`.

For example:

```bash
dotnet run --project src/Adact.Cli.Client -- --server http://<windows-host>:41300/mcp list-windows
dotnet run --project src/Adact.Cli.Client -- --server http://<windows-host>:41300/mcp attach w1
dotnet run --project src/Adact.Cli.Client -- --server http://<windows-host>:41300/mcp snapshot
```

If you installed an extracted release binary and made `adact` available in your shell, the same commands look like this:

```bash
adact --server http://<windows-host>:41300/mcp list-windows
adact --server http://<windows-host>:41300/mcp attach w1
adact --server http://<windows-host>:41300/mcp snapshot
```

To help AI coding clients discover the ADACT workflow, install the bundled skill files:

```powershell
adact install --skills claude
# or: copilot / codex
```

For the full CLI reference, see [docs/spec/cli.md](docs/spec/cli.md).

## Architecture overview

ADACT has a simple layered flow. The host and client pieces vary by OS:

```text
Windows 11 (x64 / arm64)
  AI agent / Human
    -> adact CLI
    -> adact serve daemon
    -> UI Automation engine
    -> Windows desktop app

Linux client (x64) / macOS client (arm64)
  AI agent / Human
    -> adact CLI client
    -> HTTP
    -> adact serve daemon on Windows 11
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
