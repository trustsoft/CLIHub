# CLIHub Architecture

This is the current architecture index. Detailed behavior belongs in the focused documents under
[`docs/architecture/`](architecture/); exact APIs belong to the source code and tests.

## Solution Boundaries

| Project | Target | Responsibility |
|---|---|---|
| `CLIHub.Core` | `net10.0` | Domain models, configuration, projects, plugins, agent workflows, updates, process-launch orchestration, and infrastructure ports. |
| `CLIHub.Core.Windows` | `net10.0-windows` | Windows platform adapters: registry startup, single-instance guard, theme detection, process runners, and agent process inspection. |
| `CLIHub` | `net10.0-windows` | WPF UI, tray host, startup/shutdown, hotkey, and application composition. |
| `CLIHub.Core.Tests` | `net10.0` | Core-only xUnit tests. |
| `CLIHub.Core.Windows.Tests` | `net10.0-windows` | Windows adapter tests, including real-process capture integration. |
| `CLIHub.Tests` | `net10.0-windows` | Application, ViewModel, composition, and WPF tests. |

Dependency flow:

```text
CLIHub.Core.Tests -> CLIHub.Core <- CLIHub.Core.Windows -> CLIHub <- CLIHub.Tests
```

`CLIHub.Core` has no WPF dependency and no Windows API dependency: platform integration lives behind
ports, and the adapters are in `CLIHub.Core.Windows` (see [ADR-0006](adr/0006-windows-platform-library.md)).
Windows shell conventions that need no Windows APIs — `ProcessLauncher` and command-line building —
remain in Core.

## Runtime Composition

The WPF application starts through `Program`, `App`, `ApplicationHost`, and
`ApplicationBootstrapper`. Startup loads plugins and preferences, creates the tray/launch-window session,
registers the hotkey, evaluates release notes, and starts optional update work. Shutdown removes session
subscriptions, stops tracked operations, disposes the provider, flushes persistence and logging, and releases
single-instance resources.

The launch window is the active shell. Its ViewModel composes project and agent pane controllers, command
and action coordinators, update state, and application services. `MainWindow` is retained as an unregistered
legacy reference.

Application update entry points, including Settings, share `IUpdateWorkflow` in `src/CLIHub`. Core owns Velopack
ports and update state; the application workflow owns cross-surface coordination and the existing apply policies.

## Core Ownership

- `Configuration/` owns the single `config.json` document, migrations (`Configuration/Migrations/`), snapshots,
  and atomic persistence.
- `Projects/` owns project state, selection, recency, favorites, paths, and project logos.
- `Plugins/` owns descriptor discovery, validation, catalog reload, and built-in seeding.
- `Scanning/` owns watched-root repository discovery, per-root scan coverage, the background watcher, and the
  scan status surface.
- `Agents/` owns commands, detection, versions, availability composition, command capability checks, and
  running-agent process monitoring.
- `Updates/` owns Core update/release-note contracts and Velopack integration.
- `Infrastructure/` owns process-launch orchestration, agent process matching, filesystem paths, logging
  persistence, and the infrastructure ports implemented by `CLIHub.Core.Windows`.
- `Composition/` groups Core dependency-injection registration; `CLIHub.Core.Windows` registers its own
  adapters via `AddWindowsInfrastructure`, and the application composition root calls both.

Subsystem folders define the namespace. Subfolders used only for navigation — `Models/<area>/`,
`ViewModels/<area>/`, and `Configuration/Migrations/` — keep the parent subsystem namespace so consumers
keep a single `using`; `Infrastructure/` additionally nests namespaces per area.

## Invariants

- Plugins are JSON data; no DLL loading or in-process plugin execution.
- Project folders are launch context and are not modified by project registration/removal.
- `config.json` remains one camelCase document with one atomic write boundary.
- UI code depends on narrow interfaces such as `IPreferencesStore` rather than the full configuration document.
- Interactive process launching and captured output are separate contracts (`IInteractiveProcessRunner`,
  `IProcessOutputRunner`); no compatibility aggregate is registered.
- Production Core services expose one public DI constructor.
- A running matching agent process blocks the agent update action and the action checks again before execution.

## Focused Documents

- [Startup and shutdown](architecture/startup.md)
- [Configuration persistence](architecture/configuration.md)
- [Plugin descriptors](architecture/plugins.md)
- [Project scanning](architecture/scanning.md)
- [Process execution and agent safety](architecture/processes.md)
- [WPF resources and theming](architecture/ui.md)
- [Architecture decision records](adr/README.md)

## Technology and Operations

- .NET 10, C# 14.0, WPF, H.NotifyIcon.Wpf, Microsoft.Extensions.DependencyInjection, Serilog, System.Text.Json,
  and Velopack.
- User data: `%APPDATA%\CLIHub\` with `config.json`, `logs\`, `plugins\`, and `cache\`.
- CI builds and tests on `windows-latest`; `release.yml` packages framework-dependent `win-x64` builds from
  `v*` tags and publishes them to GitHub Releases.
- Build, test, run, and OpenSpec commands are listed in [`AGENTS.md`](../AGENTS.md) and
  [`docs/project-context.md`](project-context.md).
