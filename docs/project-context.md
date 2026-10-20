# CLIHub Project Context

This is the compact, current context for AI coding agents. It summarizes stable project facts and routes
deeper questions to the canonical source. Do not treat this file as a complete API inventory; verify exact
types and signatures in the source code.

## Product

CLIHub is a Windows 10/11 WPF system-tray companion for launching AI agent CLIs in a selected project
directory. It stores project context and preferences, discovers descriptor-based agent plugins, launches
commands in an external terminal, and provides Velopack-based application updates.

The application does not embed agents, execute plugin code, act as an IDE, or support macOS/Linux.

## Solution Boundaries

| Project | Target | Responsibility |
|---|---|---|
| `src/CLIHub.Core` | `net10.0` | UI-independent domain services, models, persistence, plugins, agent workflows, and infrastructure ports. |
| `src/CLIHub.Core.Windows` | `net10.0-windows` | Windows platform adapters: registry startup, single-instance guard, theme detection, process runners, agent process inspection. |
| `src/CLIHub` | `net10.0-windows` | WPF composition root, windows, tray host, hotkey, startup/shutdown, and UI workflows. |
| `tests/CLIHub.Core.Tests` | `net10.0` | Core-only xUnit tests. |
| `tests/CLIHub.Core.Windows.Tests` | `net10.0-windows` | Windows adapter tests. |
| `tests/CLIHub.Tests` | `net10.0-windows` | Application, ViewModel, composition, and WPF-dependent tests. |

Dependency direction:

```text
CLIHub.Core.Tests -> CLIHub.Core <- CLIHub.Core.Windows -> CLIHub <- CLIHub.Tests
```

`CLIHub.Core` must not reference the WPF application or Windows-only APIs. Windows integration is isolated
in `CLIHub.Core.Windows` (registry startup, named mutex/pipe activation, process execution adapters, and
process inspection) behind ports declared in Core.

## Runtime Facts

- .NET 10 and C# 14.0 are the development baseline; nullable reference types and implicit usings are enabled.
- The application uses WPF, H.NotifyIcon.Wpf, Microsoft.Extensions.DependencyInjection, Serilog, System.Text.Json,
  and Velopack.
- Application data is under `%APPDATA%\CLIHub\`: `config.json`, `logs\`, `plugins\`, and `cache\`.
- Configuration is one camelCase JSON document with an atomic write path. Preferences and project state expose
  narrower store interfaces over that document.
- Plugins are JSON descriptors under `%APPDATA%\CLIHub\plugins\<id>\`; they contain command definitions and
  detection markers. Plugins do not load DLLs or execute code in-process.
- Project discovery watches configured roots, registers repositories as `discovered` projects, and leaves
  manually added projects untouched. Scanning settings apply immediately from the Project Discovery page.
- The Settings Projects page lists every registered project with its provenance, registers and removes
  manual projects, opens project folders, and flags missing folders; discovered projects are read-only
  there. Project changes apply immediately from that page.
- The launch window is the active WPF shell. `MainWindow` is retained as an unregistered legacy reference.
- The application enforces one instance with `Local\CLIHub.SingleInstance` and a named-pipe activation signal.
- Updates are coordinated through the application `IUpdateWorkflow`; Core owns Velopack integration and update ports.
- Agent process monitoring blocks the agent update action while any matching registered agent process is running.

## Documentation Routing

Read only the documents relevant to the task:

| Question | Canonical source |
|---|---|
| User-facing behavior and quickstart | [`README.md`](../README.md) |
| Terminology | [`docs/glossary.md`](glossary.md) |
| Current architecture and boundaries | [`docs/architecture.md`](architecture.md) |
| Startup and shutdown | [`docs/architecture/startup.md`](architecture/startup.md) |
| Configuration and persistence | [`docs/architecture/configuration.md`](architecture/configuration.md) |
| Plugin descriptor format | [`docs/architecture/plugins.md`](architecture/plugins.md) |
| Project scanning and background watcher | [`docs/architecture/scanning.md`](architecture/scanning.md) |
| Subagent model policy (which model per child task) | [`docs/agent-model-policy.md`](agent-model-policy.md) + [`docs/agent-model-policy.yaml`](agent-model-policy.yaml) |
| Process execution and agent safety | [`docs/architecture/processes.md`](architecture/processes.md) |
| WPF resources and theming | [`docs/architecture/ui.md`](architecture/ui.md) |
| Behavior contract | The relevant file under [`openspec/specs/`](../openspec/specs/) |
| Architectural rationale | [`docs/adr/`](adr/) |
| Release procedure | [`docs/releasing.md`](releasing.md) |
| Change history | [`CHANGELOG.md`](../CHANGELOG.md) and [`openspec/changes/archive/`](../openspec/changes/archive/) |

`openspec/changes/archive/`, `.opencode/`, `.pi/`, `graphify-out/`, `artifacts/`, and `obj/` are not startup
context. Read them only when the task explicitly needs historical, agent-tooling, graph, or build-output details.

## Verification

Run from the repository root:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
```

For a focused codebase question, use the repository's CodeGraph/graphify workflow before broad text searches.
