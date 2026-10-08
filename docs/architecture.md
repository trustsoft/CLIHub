# CLIHub Architecture

## Solution Structure

CLIHub is a four-project solution: `CLIHub.Core` (logic) and `CLIHub` (WPF UI) under `src/`, plus `CLIHub.Core.Tests` (Core-only) and `CLIHub.Tests` (WPF/application) under `tests/`. The full directory tree, folder inventory, and build output live in [repo-structure.md](repo-structure.md). Project responsibilities and dependency rules follow. Topic documents live under [architecture/](architecture/): [startup](architecture/startup.md), [configuration](architecture/configuration.md), [plugins](architecture/plugins.md), [processes](architecture/processes.md), and [UI](architecture/ui.md).

## Project Responsibilities

### CLIHub.Core

**Purpose:** UI-independent application logic with explicit Windows infrastructure boundaries.

**Models:** `Plugin`, `PluginCommand`, `AgentCommands`, `AgentCommandKind`, `AgentDetection`, `AgentCommandResult`, `ProcessCaptureResult`, `Project`, `RuntimeKind`/`RuntimeKinds`, `AppConfig`, `AppConfigDocument`, `AppPreferences`, `ConfigurationSnapshot`, `UpdateCheckResult`/`UpdateStatus`, `UpdateDownloadResult`/`UpdateDownloadStatus`, `UpdateControlState`, and `ReleaseNote`.

**Subsystems:**
- `Configuration/` — the `config.json` persistence document, detached snapshots, schema migrations, preference access, and the shared atomic write path
- `Projects/` — project lifecycle, `ProjectState`, path policy, and project logo resolution
- `Plugins/` — plugin discovery, validation, seeding, and plugin contracts
- `Agents/` — agent commands, detection, version lookup, availability composition, and agent contracts
- `Updates/` — update and release-note behavior and contracts
- `Infrastructure/` — process execution, filesystem paths, logo persistence, Windows startup, and single-instance integration
- `Composition/` — Core dependency injection registration grouped by subsystem

**Services:**
- `ConfigurationRepository` — JSON configuration load/save, schema validation/migration, detached snapshots, and atomic writes
- `ConfigMigrationRunner` / `LegacyTerminalPreferenceMigration` — ordered schema migration infrastructure and the version 0 to version 1 terminal preference migration
- `PreferencesStore` — preferences-only access over the shared configuration document
- `ProjectStateStore` — project-state-only access over the shared configuration document
- `ProjectService` — project tracking (current, recent, favorites, logo resolution)
- `PluginCatalog` — plugin discovery/validation from `plugins\<id>\plugin.json`; `PluginManager` remains a compatibility adapter
- `PluginSeeder` — first-run seeding of built-in descriptors + logos
- `AgentCommandService` — execute named commands (launch/resume/version/update/init)
- `AgentDetectionService` — host install + per-project availability (file checks, TTL-cached per preference)
- `AgentListComposer` — composes the launch-window agent list: drops agents that are not installed on the host, then applies the optional project-availability filter
- `AgentVersionService` — version lookup with TTL caching and a configurable probe timeout
- `UpdateService` — Velopack update check, download, and apply-and-restart (GitHub Releases source), current-version lookup, and shared download state
- `ReleaseNotesService` — parses the embedded user-facing release notes, newest version first
- `ProcessLauncher` — runtime-based spawning (Windows Terminal / Command Prompt / PowerShell) + output capture
- `StartupService` — per-user Windows Run registration for start-with-Windows (via an internal `IStartupRegistry` seam backed by `StartupRegistry`)
- `SingleInstanceGuard` — named mutex + named-pipe activation
- `DirectoryInitializer` — `%APPDATA%\CLIHub\` layout

`AppConfigDocument` is the persistence representation of the existing flat `config.json` shape. `ProjectState` owns `Projects` and `CurrentProjectId`. `ProjectStateStore` and `PreferencesStore` expose narrower access boundaries over the same single configuration document, so separating responsibility does not split the physical file or atomic write path.

**Interfaces:** `IConfigurationRepository` (shared persistence boundary), `IProjectStateStore`, `IPreferencesStore`, `IProjectService`, `IPluginCatalog`, `IPluginManager` (compatibility adapter), `IPluginSeeder`, `IAgentCommandService`, `IAgentDetectionService`, `IAgentVersionService`, `IProcessLauncher`, `IInteractiveProcessRunner`, `IProcessOutputRunner`, `IUpdateService`, `IReleaseNotesService`, `IStartupService`.

**Utilities:** `HotkeyParser`/`HotkeyModifiers`/`HotkeyDefinition`, `LoggingSetup`/`LogLevelParser`/`PreferenceReader`, `MiddleEllipsisFormatter` and `PathLeftTrimFormatter` (path shortening for display, selected by `PathDisplayStyle`/`PathDisplayStyles`), `ServiceCollectionExtensions` (`AddClIHubCoreServices`).

**Dependencies:** .NET 10, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging`, `Serilog`, `System.Text.Json`, `Velopack`. No WPF dependency. Core is UI-independent, but Windows-aware: Registry, named mutexes/pipes, Windows Terminal, and process integration live under `Infrastructure/`.

**Target:** `net10.0`.

### CLIHub

**Purpose:** WPF user interface and Windows-specific integration.

**Contains:** app entry/DI wiring (`Program` with the Velopack bootstrap, `App.xaml(.cs)`, `ApplicationHost`, `ServiceRegistration`), startup coordinators (`InstanceCoordinator`, `StartupStateLoader`, `OptionalStartupCoordinator`, `ApplicationBootstrapper`), `TrayIconController` (H.NotifyIcon.Wpf), `ISettingsLauncher`/`SettingsLauncher` (owns the single Settings window instance), `PromptState` (keeps the popup visible while a prompt is open), `LaunchWindow` + `LaunchWindowViewModel` (the chromeless popup shell: no OS chrome, always on top, hides when it loses focus unless pinned, Escape hides it, centred on the pointer's monitor on every show; shown from the tray, the hotkey, a second-instance activation and startup; the pane Actions menus are data-driven — `MenuAction` entries (label, glyph, command, availability, checked state) defined in the view model and rendered by shared styles), `MainWindow` (retained from the pre-redesign scaffold; kept for reference, no longer registered or constructed), `AgentItem`, `GlobalHotkeyService`, `PathToImageConverter`, `PathDisplayConverter`, `Themes/` (`LaunchTheme.xaml` palette — the single place for colors; `Sizing.xaml` metrics and tokens such as the icon font family; `Controls.xaml` shared keyed styles — button templates (`IconChipButton`, `QuietChipButton` and their derivatives), drawn-window chrome (`WindowHeader`, `WindowTitle`, `WindowCloseButton`), common typography (`SectionLabel`, `BrandText`, `VersionChip*`) and the `DarkToolTip` base style; `DarkScrollBar.xaml` shared dark-control style; `SettingsStyles.xaml`, `LaunchWindowStyles.xaml` and `WhatsNewStyles.xaml` window-scoped styles that merge `Controls.xaml`, which brings the palette and sizing tokens transitively (StaticResource inside a dictionary resolves only against that dictionary's own merged dictionaries). `App.xaml` hosts the implicit dark ToolTip style — tooltips, like scrollbars, live outside the window visual tree, so only an application-level implicit style reaches them — and the shared converter instances (`PathToImage`, `PathDisplay`, `BoolToVisibility`), `Interop/User32` (source-generated P/Invoke).

**Dependencies:** CLIHub.Core, WPF, H.NotifyIcon.Wpf, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Serilog.

The WPF composition root registers `ApplicationHost` (not in DI, created by `App`), `SingleInstanceGuard` (owner of the process mutex and activation pipe), startup coordinators (`InstanceCoordinator`, `StartupStateLoader`, `OptionalStartupCoordinator`), `ApplicationBootstrapper`, `ApplicationSession`, `LaunchCommandCoordinator`, `LaunchWindowActionBuilder`, `ExternalLauncher`, `TrayStateProjection`, `TrayCommandHandlers`, `PluginInitializationService`, `TrayMenuBuilder`, `LaunchWindowViewModel`, `LaunchWindow`, `TrayIconController`, settings and release-notes launchers, and the global hotkey services. `ApplicationHost` owns the WPF lifecycle boundary: environment setup (directories, logging), DI composition, and shutdown (session disposal, operation stop, provider disposal, log flush). `ApplicationSession` owns application-level startup subscriptions and removes them before tracked operations stop. `LaunchWindowViewModel` composes the pane controllers, command coordinator, action builder, external launcher, and shared update control while retaining binding-facing state. Tray state projection and tray command execution are independently owned by `TrayStateProjection` and `TrayCommandHandlers`; `TrayActions` adapts them to the stable host contract. Pane controllers, update control, and launch-window composition model implement deterministic disposal for their own subscriptions. `MainWindow` remains a deprecated legacy reference window and is not registered or constructed.

**Target:** `net10.0-windows`.

Update operations are coordinated by the singleton `IUpdateWorkflow` / `UpdateWorkflow` in the application project. It wraps the existing Core update ports, forwards availability state, and owns shared checking state. Concurrent checks join one task; the initiating caller supplies the operation token, while cancellation by a joining caller only cancels that caller's wait. The initiating operation remains tracked until the underlying check finishes. Settings retains its narrow checker contract, wired to the same workflow at the composition root.

`UpdateStartupCoordinator` applies the startup preference and notification policy. Tray and What's New download requests reach `DownloadAndApplyAsync` through `ApplicationSession`; workflow outcome events are dispatched to the tray, followed by the existing two-second delay and automatic restart. A download reservation spans that delay to prevent another download/apply flow. The launch-window control uses `DownloadUpdateAsync` and keeps its explicit restart action after success. Core `UpdateService` still owns Velopack integration and availability; no Core contracts or persistence formats change.

### CLIHub.Core.Tests

**Purpose:** Core-only unit and composition tests (xUnit).

**Contains:** Core service, model, formatting, logging, hotkey, release-notes, infrastructure, plugin, and configuration tests.

**Dependencies:** `CLIHub.Core`, xUnit, Microsoft.NET.Test.Sdk, Microsoft.Extensions.DependencyInjection, Moq, and coverlet.

**Target:** `net10.0`; no WPF reference.

### CLIHub.Tests

**Purpose:** WPF/application-specific tests (xUnit).

**Contains:** application composition, startup orchestration, workflows, ViewModels, and WPF-dependent tests.

**Dependencies:** `CLIHub` and `CLIHub.Core`, xUnit, Microsoft.NET.Test.Sdk, Microsoft.Extensions.DependencyInjection, Moq, and coverlet.

**Target:** `net10.0-windows` with WPF enabled.

## Application Startup and Shutdown

Startup and shutdown sequence, startup failure policy, and disposal order: [architecture/startup.md](architecture/startup.md).

## Dependency Flow

```
CLIHub.Core.Tests ──> CLIHub.Core <── CLIHub
CLIHub.Tests ────────> CLIHub ──> CLIHub.Core
                                   │
                                   ├─> System Tray (H.NotifyIcon.Wpf)
                                   ├─> Windows Terminal (wt.exe)
                                   └─> Global hotkey (RegisterHotKey)
```

**Rules:**
- `CLIHub.Core` has NO dependency on `CLIHub` (UI)
- `CLIHub.Core.Tests` references `CLIHub.Core` only and does not enable WPF
- `CLIHub.Tests` references both `CLIHub` and `CLIHub.Core` for application/UI coverage
- Business logic lives in `CLIHub.Core` for testability
- `CLIHub` is a thin presentation layer over Core services
- Core subsystem dependencies use interfaces or explicit state boundaries; infrastructure does not depend on application facades
- Project state is mutated by the project subsystem; unrelated services do not mutate `Projects` or `CurrentProjectId`
- UI components use `IPreferencesStore` when they need preferences and do not depend on the full `IConfigurationRepository` document.
- The existing flat `config.json` format and one atomic persistence path remain stable during the boundary refactor
- Interactive process launching and captured command output are separate process contracts; `IProcessLauncher` remains the compatibility aggregate
- Each production Core service exposes one public constructor for dependency injection; test-only seams use explicit internal factories or adapters

## Capabilities (per `openspec/specs/`)

`app-lifecycle`, `logging`, `project-management`, `plugin-seeding`, `logo-cache`, `agent-commands`, `agent-detection`, `agent-version`, `agent-availability-display`, `hotkey-support`, `update-checking`, `main-window-layout`, `launch-window-theme`, `settings-theme`, `preferences-ui`, `release-notes`, `release-notes-display`, `ci-build`, `release-pipeline`, `code-style`. Each spec defines observable behavior; see the corresponding spec for requirements.

## Decision Records

Architectural decisions and the trade-offs behind them are recorded as ADRs: [adr/README.md](adr/README.md).

## Technology Stack

### Core Technologies
- **.NET 10 LTS** — long-term support
- **C# 14.0** with nullable reference types and implicit usings enabled; the version is declared centrally in `Directory.Build.props`
- **WPF** — Windows Presentation Foundation for UI

### Libraries
- **Microsoft.Extensions.DependencyInjection** — service container
- **Serilog** with file sink (+ `Serilog.Extensions.Logging`) — structured logging, bridged into `Microsoft.Extensions.Logging`
- **H.NotifyIcon.Wpf** — system tray icon
- **System.Text.Json** — JSON serialization with camelCase policy
- **Velopack** — application update checking, downloading, and packaging (GitHub Releases source); packaging runs in CI, see [Packaging & CI/CD](#packaging-cicd)

### External Integration

Runtime spawning, single-instance activation, hotkey registration, update source, and start-with-Windows integration details: [architecture/processes.md](architecture/processes.md).

## Plugin Descriptor Format

Descriptor JSON format, commands, detection markers, and built-in seeding: [architecture/plugins.md](architecture/plugins.md).

## Release Notes

- **Files:** `CHANGELOG.md` (technical, for developers) and `RELEASE-NOTES.md` (user-facing) at the repository root; both use the same `## <version> — <date>` headings so a release lines up across them.
- **Audience split and rationale:** see [changelog-and-release-notes.md](changelog-and-release-notes.md).
- **Format** of `RELEASE-NOTES.md` (the application parses only this one):
  - a version section heading `## <version> — <date>`, where the separator may be an em dash, an en dash, or a spaced hyphen (so a prerelease such as `0.6.0-beta.1` is never split at its hyphen);
  - the groups `### New`, `### Improved`, and `### Fixed`, whose entries are `-`/`*` list items; a missing group is simply empty;
  - anything else — the title, the preamble, prose outside a group, an unknown group, a deeper heading — is ignored;
  - a line that is neither a heading nor a list item continues the previous entry, so a hand-wrapped sentence stays one entry.
- **Authoring rules:** entries are plain text — no Markdown emphasis, since nothing renders it — and one entry per list item, written in short user-facing sentences. Markdown is fine in `CHANGELOG.md`, which is not rendered in the application.
- **Delivery:** `RELEASE-NOTES.md` is embedded into `CLIHub.Core` by link with the logical name `CLIHub.Core.ReleaseNotes.RELEASE-NOTES.md`, so the notes are available offline and independently of the update mechanism. There is no duplicate copy under `src/`.
- **Parsing:** `ReleaseNotesService` (`IReleaseNotesService`) reads the embedded document once, orders notes by version descending (`System.Version`), and keeps a version that does not parse in its document position at the end rather than failing. Malformed, empty, or missing notes are logged and yield no notes; the application continues normally.
- **Failure modes:** an unparseable version heading drops that section only, and the remaining sections still parse.

## Packaging & CI/CD

Two GitHub Actions workflows, both on `windows-latest` (WPF does not build on Linux). The
step-by-step release procedure lives in [releasing.md](releasing.md); this section describes the
machinery:

- **`ci.yml`** — every pull request and every push to `master`: `dotnet build CLIHub.sln -c Release`, then `dotnet test`.
- **`release.yml`** — triggered by pushing a tag `v*`:
  1. a `test` job builds and tests the solution as a gate;
  2. a `release` job (`permissions: contents: write`) derives the version from the tag (leading `v` stripped; the tag is the source of truth — `Directory.Build.props` stays the development default), extracts the released version's section from `RELEASE-NOTES.md` (fails the job when the section is missing), and publishes framework-dependent `win-x64` with `-p:Version=<tag version>`;
  3. `vpk pack` (the Velopack CLI tool, pinned to the exact Velopack library version the app links) produces the setup, portable, and delta assets with `--runtime win-x64 --framework net10.0-x64-desktop` — the installer offers the .NET 10 Desktop Runtime when it is missing — and the notes file;
  4. `vpk upload github` publishes everything to the repository's GitHub Releases as a published release on the pushed tag (`--merge true`, so re-running for the same tag updates the release). `GITHUB_TOKEN` authenticates the upload; the extracted notes section becomes the release body and the updater's notes.

- **Update channel:** stable only (the default `win` channel). Prerelease channels are not wired.
- **Signing:** the packages are unsigned (no certificate yet); SmartScreen may warn on first install.
- **User prerequisite:** the .NET 10 Desktop Runtime (x64), unless the installer's runtime bootstrap installs it.
- **Rollback:** delete the GitHub release and the tag; installed clients then never see the update.

## File System Layout

```
%APPDATA%\CLIHub\
├── config.json              (application configuration)
├── logs\
│   └── clihub-YYYYMMDD.log (daily log files, 7-day retention)
├── plugins\
│   ├── opencode\
│   │   ├── plugin.json     (descriptor)
│   │   └── logo.png        (seeded / optional)
│   └── ...                 (pi, cline-cli, github-copilot, openclaude, qwen-code)
└── cache\                   (reserved for future use)
```

## Configuration Persistence

Format, location, atomic writes, schema, and forward compatibility: [architecture/configuration.md](architecture/configuration.md).

## Logging Strategy

- **Framework:** Serilog with file sink, bridged into `Microsoft.Extensions.Logging` (`ILogger<T>` injected via DI)
- **Location:** `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log`
- **Rotation:** daily files, 7-file retention
- **Levels:** configurable via `AppPreferences.LogLevel` (default `Information`); `Microsoft.*` and `System.*` overridden to `Warning`
- **Format:** `[timestamp level] message` with exception details
- **Shutdown:** `Log.CloseAndFlush()` on exit

## Error Handling Strategy

- **Configuration errors:** [architecture/configuration.md](architecture/configuration.md).
- **Plugin errors:** [architecture/plugins.md](architecture/plugins.md).
- **Process and agent errors:** [architecture/processes.md](architecture/processes.md).

### System Integration Errors

- **Second instance:** signal the running instance (named pipe) and exit; the running instance shows its window
- **Hotkey unavailable:** log a warning and continue without a hotkey
- **Seeding failure:** log and continue
- **Update check failure/timeout:** log and continue; reported as a failed check, never blocking the app

## Testing Strategy

**Unit tests (CLIHub.Tests):** project tracking/logo resolution, plugin discovery/validation, seeding, agent command routing, availability detection, agent list composition (uninstalled agents excluded, then the project filter), version extraction/caching, update check status/version handling and download guard paths (no update, not installed, already downloading, never throws), release-notes parsing (ordering, groups, malformed headings, embedded document), process output capture, logging setup/level parsing, path shortening (middle ellipsis, left trim, style parsing), hotkey parsing, single-instance guard, DI composition.

**Manual verification:** system tray behavior, window show/hide and hotkey toggle, the popup shell (chromeless chrome, hide on focus loss, pin, Escape, pointer-monitor placement), agent launch in the selected runtime, seeded logos/versions, availability display (agents without a system marker are not listed; project-unavailable agents dimmed or filtered), clicking the hotkey field focuses it for capture, the launch window (dark theme, pane headers with Actions menus, footer actions, version chip, update check, open data folder, project/agent rows, path display style, scrolling), the update-available tray notification, the Settings window (dark drawn theme, segmented selectors, hotkey chips; runtime/hotkey/probe/updates/path-display changes applied without restart; Cancel discards), the What's New window (tray entry, notes newest first in the drawn dark chrome, header drag, resize, Escape and the close button), the one-time release-notes display after a version change, the download-and-restart update flow (tray menu item, downloading state, completion and failure balloons, restart into the new version, What's New install action), and start-with-Windows plus window-visibility-on-startup.

## Conventions

### Code Style

See [AGENTS.md → Code Style](../AGENTS.md#code-style) (file-scoped namespaces, nullable, implicit usings, braces, naming, async, MVVM).

The rules live in the root [`.editorconfig`](../.editorconfig) and are enforced at build time: `EnforceCodeStyleInBuild` in `Directory.Build.props` plus explicit severity pins surface naming (private fields, `I`-prefixed interfaces), brace, namespace, and using-placement violations as build warnings (the `code-style` spec). The application projects also enable `GenerateDocumentationFile`, so the compiler reports malformed XML doc comments and `<param>` tags that drift from the actual signature.

### Namespace Structure
- `CLIHub.Core.Models`, `CLIHub.Core.Hotkeys`, `CLIHub.Core.Logging`, `CLIHub.Core.Formatting`
- `CLIHub.Core.Configuration`, `CLIHub.Core.Projects`, `CLIHub.Core.Plugins`, `CLIHub.Core.Agents`, `CLIHub.Core.Updates`
- `CLIHub.Core.Infrastructure.FileSystem`, `CLIHub.Core.Infrastructure.Persistence`, `CLIHub.Core.Infrastructure.Processes`, `CLIHub.Core.Infrastructure.Windows`, `CLIHub.Core.Composition`
- Core contracts live beside their owning subsystem; the former `CLIHub.Core.Services` and `CLIHub.Core.Interfaces` namespaces are no longer used. Consumers of the public Core source API must update their using directives after this source-level namespace migration.
- `CLIHub` (App, controllers), `CLIHub.Views`, `CLIHub.Hotkeys`, `CLIHub.Interop`, `CLIHub.Converters`, `CLIHub.ViewModels`
- `CLIHub.Themes` (XAML resource dictionaries; the only code is `IconGlyphs`, the compile-time checked Segoe MDL2 glyph constants referenced from XAML via `{x:Static themes:IconGlyphs.Name}`)

### UI Resources and Theming

Resource organization and the XAML themes/styles recipe: [architecture/ui.md](architecture/ui.md).

## Build and Run

```powershell
# Restore, build, test
dotnet build CLIHub.sln
dotnet test CLIHub.sln

# Run the application
dotnet run --project src/CLIHub/CLIHub.csproj
```

## Known Constraints

See [AGENTS.md → Known Constraints](../AGENTS.md#known-constraints). The single-instance and security implications are detailed under [Security Considerations](#security-considerations) below.

## Security Considerations

### Single Instance Enforcement

- **Mechanism:** named mutex `Local\CLIHub.SingleInstance` created at startup, plus a named pipe (`CLIHub.SingleInstance`)
- **Behavior:** the first instance acquires the mutex and listens on the pipe; later instances signal it to show the window and exit
- **Scope:** `Local\` (per session). A standard user can create `Local\` mutexes reliably, and a tray companion is inherently per-user, so session scope is the correct boundary (`Global\` would span user sessions and can require elevated rights)

### File System Security

- **Config directory permissions:** default Windows ACLs for `%APPDATA%\CLIHub\` (user-only read/write)
- **Atomic writes:** temp-file-then-rename prevents corruption from crashes or power loss
- **Log/plugin access:** restricted to the current user via `%APPDATA%`

### Process Spawning Security

- **Argument handling:** the version command runs through the command interpreter (`%COMSPEC% /c`) so npm shims resolve; interactive commands spawn Windows Terminal with the project folder as the working directory
- **Working directory validation:** the directory must exist before spawning
- **No elevation:** CLIHub runs with standard user privileges and never requests admin rights

### Secrets and Credentials

- **No credential storage:** CLIHub does not store API keys, tokens, or passwords
- **Agent credentials:** the responsibility of the individual CLI tools
- **Config contents:** project paths and preferences only

### Update Mechanism

- **Checks:** startup and manual checks run through Velopack against the configured GitHub Releases source over HTTPS; failures and timeouts are logged and never block the app
- **Download and apply:** when a check finds an update, the tray menu and the What's New window offer a one-click "Download and restart" action; the download runs once (concurrent requests are ignored), a balloon announces the restart, and the app applies the downloaded package and relaunches
- **Failure handling:** a failed download or restart is logged and reported via a tray notification; the current version keeps running and the action becomes available again
- **Not installed:** when the app does not run from a Velopack install, checks are skipped, no update action is offered, and results are reported as `NotInstalled`
