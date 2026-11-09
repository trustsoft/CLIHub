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
CLIHub.Core.Tests -> CLIHub.Core <- CLIHub.Core.Windows <- CLIHub <- CLIHub.Tests
CLIHub.Core.Windows.Tests -> CLIHub.Core.Windows, CLIHub.Core
```

An arrow points from a project to the project it references.

`CLIHub.Core` must not reference the WPF application or Windows-only APIs. Windows integration is isolated
in `CLIHub.Core.Windows` (registry startup, named mutex/pipe activation, process execution adapters, and
process inspection) behind ports declared in Core.

## Runtime Facts

- .NET 10 and C# 14.0 are the development baseline; nullable reference types and implicit usings are enabled.
- The application uses WPF, H.NotifyIcon.Wpf, Microsoft.Extensions.DependencyInjection, Serilog, System.Text.Json,
  and Velopack.
- Application data is under `%APPDATA%\CLIHub\`: `config.json`, `plugin-state.json`, `logs\`, `plugins\`, and `cache\`.
- Configuration is one camelCase JSON document with an atomic write path. Preferences and project state expose
  narrower store interfaces over that document.
- Plugin lifecycle state (origin, content hash, disabled flag) is tracked in a separate `plugin-state.json` under
  `%APPDATA%\CLIHub\`.
- Plugins are JSON descriptors under `%APPDATA%\CLIHub\plugins\<id>\`; they contain command definitions and
  detection markers. Plugins do not load DLLs or execute code in-process.
- Users can install plugins from local `.clihub-plugin.zip` packages, browse and install from the remote HTTPS
  plugin feed (default: official `trustsoft/CLIHub-plugins` index), disable/enable plugins, and open the plugins
  folder from the CLI Agents settings page. Built-in plugins are refreshed on app update using three-hash logic
  that preserves user modifications. The feed URL preference (absent/null = default official; explicit `""` = disabled;
  non-empty = custom HTTPS URL) controls Browse Plugins visibility.
- Project discovery watches configured roots, registers repositories as `discovered` projects, and leaves
  manually added projects untouched. Scanning settings apply immediately from the Project Scanning page.
- The Settings Projects page lists every registered project with its provenance, registers, removes, and
  changes the folder of manual projects, opens project folders, and flags missing folders; discovered
  projects are read-only there. Project changes apply immediately from that page.
- The Settings sidebar has 10 pages in three groups (General, Engines & Repos, System). The global hotkey,
  launch-window position, and path display are configured on the Launch Window page; the probe cache TTL
  and version-check timeout are configured on Agent Detection.
- The launch window is the active WPF shell; project rows show favorite and provenance markers in a
  right-side rail, and favorites sort first in the project list.
- CLI Agents cards show a version/state chip that an update chip replaces while an update is available;
  plugin origin plus descriptor metadata live in the agent name's tooltip, not in a separate badge.
  The AGENTS cards list installed agents first and not-installed agents last, preserving the plugin
  catalog order within each group; the order updates when a Detect settles an agent's installation state.
- Appearance is Dark, Light, or Deep Blue, plus System, which follows the Windows light/dark mode; one
  application-scoped accent color (Sky by default) is independent of the theme. Theme and accent changes
  apply immediately on a Settings save. With the System theme, a Windows personalization change is
  followed while the application runs and re-resolves the palette; fixed themes ignore it.
- The application enforces one instance with `Local\CLIHub.SingleInstance` and a named-pipe activation signal.
- Update checking is stable-only (no pre-release channel); updates are coordinated through the application
  `IUpdateWorkflow`, while Core owns Velopack integration and update ports.
- The Settings → CLI Agents card Update action is enabled only when CLIHub knows an update is available
  (`UpdateAvailable`), the agent declares an update command, and no matching registered agent process is running.

## Documentation Routing

Read only the documents relevant to the task:

| Question | Canonical source |
|---|---|
| User-facing behavior and quickstart | [`README.md`](../README.md) |
| Terminology | [`docs/glossary.md`](glossary.md) |
| Current architecture and boundaries | [`docs/architecture.md`](architecture.md) |
| Repository layout and source navigation | [`docs/repo-structure.md`](repo-structure.md) |
| Startup and shutdown | [`docs/architecture/startup.md`](architecture/startup.md) |
| Configuration and persistence | [`docs/architecture/configuration.md`](architecture/configuration.md) |
| Plugin descriptor format | [`docs/architecture/plugins.md`](architecture/plugins.md) |
| Project scanning and background watcher | [`docs/architecture/scanning.md`](architecture/scanning.md) |
| Subagent model policy (which model per child task) | [`docs/agent-model-policy.md`](agent-model-policy.md) + [`docs/agent-model-policy.json`](agent-model-policy.json) |
| Process execution and agent safety | [`docs/architecture/processes.md`](architecture/processes.md) |
| WPF resources and theming | [`docs/architecture/ui.md`](architecture/ui.md) |
| Testing conventions and gates | [`docs/testing.md`](testing.md) |
| Behavior contract | The relevant file under [`openspec/specs/`](../openspec/specs/) |
| Architectural rationale | [`docs/adr/`](adr/) |
| Parked findings and deferred work | [`docs/backlog.md`](backlog.md) |
| Product vision and non-goals | [`docs/vision.md`](vision.md) |
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
