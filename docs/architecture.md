# CLIHub Architecture

This is the current architecture index. Detailed behavior belongs in the focused documents under
[`docs/architecture/`](architecture/); exact APIs belong to the source code and tests.

## Solution Boundaries

| Project | Target | Responsibility |
|---|---|---|
| `CLIHub.Core` | `net10.0` | Domain models, configuration, projects, plugins, agent workflows, updates, and Windows infrastructure. |
| `CLIHub` | `net10.0-windows` | WPF UI, tray host, startup/shutdown, hotkey, and application composition. |
| `CLIHub.Core.Tests` | `net10.0` | Core-only xUnit tests. |
| `CLIHub.Tests` | `net10.0-windows` | Application, ViewModel, composition, and WPF tests. |

Dependency flow:

```text
CLIHub.Core.Tests -> CLIHub.Core <- CLIHub <- CLIHub.Tests
```

`CLIHub.Core` has no WPF dependency. It is UI-independent but Windows-aware: registry startup, named
mutex/pipe activation, process execution, and agent process inspection live behind Core infrastructure
boundaries.

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

- `Configuration/` owns the single `config.json` document, migrations, snapshots, and atomic persistence.
- `Projects/` owns project state, selection, recency, favorites, paths, and project logos.
- `Plugins/` owns descriptor discovery, validation, catalog reload, and built-in seeding.
- `Agents/` owns commands, detection, versions, availability composition, and command capability checks.
- `Updates/` owns Core update/release-note contracts and Velopack integration.
- `Infrastructure/` owns process runners, agent process inspection/matching, filesystem paths, logging
  persistence, Windows startup, and single-instance integration.
- `Composition/` groups Core dependency-injection registration.

## Invariants

- Plugins are JSON data; no DLL loading or in-process plugin execution.
- Project folders are launch context and are not modified by project registration/removal.
- `config.json` remains one camelCase document with one atomic write boundary.
- UI code depends on narrow interfaces such as `IPreferencesStore` rather than the full configuration document.
- Interactive process launching and captured output are separate contracts; `IProcessLauncher` is the
  compatibility aggregate.
- Production Core services expose one public DI constructor.
- A running matching agent process blocks the agent update action and the action checks again before execution.

## Focused Documents

- [Startup and shutdown](architecture/startup.md)
- [Configuration persistence](architecture/configuration.md)
- [Plugin descriptors](architecture/plugins.md)
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
