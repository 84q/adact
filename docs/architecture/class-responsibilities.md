# Class Responsibilities

This document organizes the main ADACT classes by the state they hold, the calls they make, and the values they return. For CLI/MCP I/O details, see [../spec/cli.md](../spec/cli.md) and [../spec/mcp-tools.md](../spec/mcp-tools.md).

## Layers

| Layer | Main responsibility |
| --- | --- |
| CLI entry / command layer | Define `adact <subcommand>`, validate arguments, call MCP tools, and shape CLI output |
| Connection / output / snapshot layer | Resolve daemon connections, call MCP, and format stdout/stderr and CLI snapshots |
| Server host layer | Start the HTTP / Named Pipe MCP server and abstract daemon shutdown |
| MCP common layer | Implement `adact_*` tools and manage session/window refs |
| Engine layer | Perform UIA operations, maintain window sessions, build raw snapshot JSON, and resolve element refs |

## CLI entry / command layer

| Type | Holds | Calls | Returns |
| --- | --- | --- | --- |
| `Program` | Root command setup | `Build()`, `Parse()`, `InvokeAsync()` | Process exit code |
| `ListWindowsCommand` | `--server` | `RunWithClientAsync()`, `adact_list_windows`, `TsvWriter` | Window list TSV |
| `AttachCommand` | `ref`, options, `--no-snapshot`, `--snapshot-dir`, `--server` | `RefValidator`, `adact_attach`, `WriteSnapshotResultAsync()` | `sessionId`, `windowRef`, snapshot path |
| `SnapshotCommand` | `--sid`, `--snapshot-dir`, `--filter`, `--server` | `WriteSnapshotResultAsync()` | `sessionId`, snapshot path |
| `ClickCommand` / `FillCommand` | Element ref, action options, snapshot options, `--server` | `RefValidator`, `RunRefOperationAndAutoSnapshotAsync()`, `adact_click` / `adact_fill` | Snapshot path or `sessionId` |
| Other command builders | Keyboard, mouse, toggle, window, inspect, screenshot, wait, launch, lifecycle, daemon stop, install, serve | Matching MCP tools or host entry points | Command-specific result |
| `CommandHelpers` | Shared options and execution helpers | `ConnectionResolver`, `NamedPipeMcpClient`, `AdactMcpClient`, `McpResponse`, snapshot pipeline | Command-specific exit code |
| `RefValidator` | `w<n>`, `s<n>`, `s<n>e<n>` patterns | CLI validation and session extraction | Boolean / session id |

The CLI layer is thin. It validates arguments, selects MCP tool names and arguments, and keeps daemon-owned session/ref state out of the process.

## Connection / output / snapshot layer

| Type | Holds | Calls | Returns |
| --- | --- | --- | --- |
| `ConnectionResolver` | Default URL and resolution order | `ServerEndpoint.Parse()`, config lookup | Resolved endpoint |
| `NamedPipeMcpClient` / `AdactMcpClient` | MCP SDK client and endpoint | Named Pipe or HTTP transport, `CallToolAsync()` | `CallToolResult` |
| `McpResponse` | No persistent state | Extracts structured/text JSON or reports CLI errors | `JsonElement` or exit code |
| `KeyValueWriter` / `TsvWriter` | No persistent state | `Console.Out` | Key-value or TSV output |
| `SnapshotJsonParser` | No persistent state | Raw snapshot JSON parse | `SnapshotMeta` and `SnapshotElement` tree |
| `SnapshotTreeFilter` | Filter name and rules | Recursive tree filtering | `raw` or `operable` tree |
| `SnapshotTextFormatter` | No persistent state | Frontmatter and tree formatting | Playwright-style `.txt` snapshot |
| `SnapshotFileWriter` | Output directory and filename rules | Directory creation and UTF-8 write | Relative snapshot path |

## Server host layer

| Type | Holds | Calls | Returns |
| --- | --- | --- | --- |
| `HttpHost` | `/mcp` path and interactive-session guard | `InteractiveSessionGuard.Probe()`, DI registration, Kestrel startup | Daemon exit code |
| `HttpDaemonControl` | `IHostApplicationLifetime` | `StopApplication()` | Stop task for `adact_daemon_stop` |
| `NamedPipeHost` | Named Pipe endpoint, pipe name, and interactive-session guard | `InteractiveSessionGuard.Probe()`, DI registration, pipe listener startup | Daemon exit code |
| `NamedPipeDaemonControl` | `CancellationTokenSource` | `Cancel()` | Stop task for `adact_daemon_stop` |

## MCP common layer

| Type | Holds | Calls | Returns |
| --- | --- | --- | --- |
| `WindowsTools` | `SessionStore`, `WindowRefStore`, `IDaemonControl`, logger | `UiaEngine`, `WindowSession`, stores, `ToolErrors` | MCP `CallToolResult` |
| `SessionStore` | `s<n>` to `WindowSession`, active session, tool lock | Session add/remove/find and ref-to-session resolution | `WindowSession` or session list |
| `WindowRefStore` | `WindowKey` to `WindowRefEntry` and next `w<n>` value | List synchronization, retire, session association | `windowRef` entry |
| `ToolErrors` | Error code constants | Maps Engine exceptions to MCP errors | Error-shaped `CallToolResult` |
| `IDaemonControl` | Stop capability | Delegates to HTTP implementation | Stop task |

`WindowsTools` is the MCP boundary. It serializes tool calls, resolves sessions, and converts Engine exceptions into MCP tool errors.

## Engine layer

| Type | Holds | Calls | Returns |
| --- | --- | --- | --- |
| `UiaEngine` | `UIA3Automation`, shared gate, next session number, logger | Window enumeration, attach, and window-session creation | `WindowInfo` list, `WindowSession` |
| `WindowSession` | Target window, root `IElement`, `RefRegistry`, shared gate, metadata | Snapshot builder, ref resolution, click/fill/close/kill | `SnapshotResult`, operation task |
| `InteractiveSessionGuard` | Desktop/session rules | Session and window-station checks | Startup allowance and diagnostics |
| `IElement` / `FlaUiElement` | UIA abstraction and wrapper state | Property and input APIs | Properties, children, and action results |
| `SnapshotBuilder` | Session `RefRegistry` | DFS build over the root/modal tree | Raw JSON and session id |
| `RefRegistry` | Stable key to `eid`, current snapshot element map | Ref allocation and resolution | Stable `elementRef` values |
| `MouseTarget`, `WaitFor*`, `Launch*`, `InspectResult`, `ScreenshotResult`, exceptions | Operation-specific value types and errors | Used by mouse, wait, launch, inspect, and screenshot flows | Tool inputs/outputs and mapped errors |

The Engine absorbs UIA instability. `UiaEngine` and `WindowSession` share the same gate so window enumeration, attach, snapshot, click, fill, close, and kill run one at a time in the daemon process.

## Dependency direction

### Build-time dependencies

| From | To | Purpose |
| --- | --- | --- |
| `Adact.Cli` | `Adact.Cli.Server` | Start `adact serve http` or `adact serve pipe` |
| `Adact.Cli` | `Adact.Engine` | Build-time reference only |
| `Adact.Cli` | `Adact.Mcp.Common` | Build-time reference only |
| `Adact.Cli.Server` | `Adact.Mcp.Common`, `Adact.Engine` | HTTP / Named Pipe daemon DI setup |
| `Adact.Mcp.Common` | `Adact.Engine` | UIA operations and exception mapping |

### Runtime call flow

| From | To | Purpose |
| --- | --- | --- |
| CLI command | `NamedPipeMcpClient` / `AdactMcpClient` | Pass tool name and arguments after validation |
| `NamedPipeMcpClient` / `AdactMcpClient` | MCP daemon | Call tools over Named Pipe or HTTP |
| MCP daemon | `WindowsTools` | Dispatch to MCP tool implementation |
| `WindowsTools` | `Adact.Engine` | Perform UIA operations |

The normal CLI path is `NamedPipeMcpClient` -> MCP daemon -> `WindowsTools` -> Engine, with `AdactMcpClient` used when `--server` selects HTTP.

## Related documents

| Document | Description |
| --- | --- |
| [command-flows.md](command-flows.md) | CLI subcommand flow |
| [snapshot-pipeline.md](snapshot-pipeline.md) | Snapshot generation and ref handling |
| [../spec/ref-ids.md](../spec/ref-ids.md) | Ref formats and invalidation rules |
