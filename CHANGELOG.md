# Changelog

The technical record of what changed in CLIHub: what, why, and which capability spec it belongs to.
User-facing notes for the same releases live in [RELEASE-NOTES.md](RELEASE-NOTES.md); both files use the
same version heading (`## <version> — <date>`) so a release lines up across them.

Groupings: `Added`, `Changed`, `Fixed`, `Removed`. A change that breaks existing behavior or
configuration is marked `**BREAKING**`. Capability names in parentheses refer to `openspec/specs/<name>`.

## 0.9.5 — 2026-10-10

### Added

- Capability-aware agent command UI: the launch window reflects each agent's supported commands (`agent-ui`)
- Agent process monitoring that blocks the agent update action while a matching process is running (`agent-process-monitoring`)
- Plugin catalog reload consumer that refreshes the agent list (`plugin-seeding`)
- Configuration repository with immutable snapshots, a schema version, and a migration runner (`configuration-snapshot`)
- Application operation lifetime for tracked background work, plus the shell coordinator and application host/session decomposition (`app-lifecycle`, `shell-coordinator`)
- Architecture dependency-boundary enforcement tests and architecture decision records

### Changed

- All update entry points route through one shared update workflow, and the update service is split into focused checker, downloader, installer, and provider components (`update-checking`)
- Core configuration boundary hardened while keeping the single camelCase document and its one atomic write path (`configuration-snapshot`)
- Process subsystem refined around cancellation-safe interactive and output runners (`processes`)
- Tray split into a state projection and command handlers, with tray actions separated from the host
- Launch window view model decomposed into pane, menu, status-message, and window-action coordinators

### Removed

- The `IProcessLauncher` compatibility aggregate, replaced by the interactive and output process runner contracts (`processes`)

## 0.9.0 — 2026-10-05

### Added

- Explicit C# 14.0 language policy and stable .NET 10 SDK resolution with `global.json` and controlled feature-band roll-forward (`code-style`)

### Changed

- **BREAKING**: `CLIHub.Core`, the WPF application, and the test project now target .NET 10 (`ci-build`, `release-pipeline`)
- Direct runtime, tray, logging, update, and test dependencies refreshed to compatible current releases
- CI and release workflows now install the .NET 10 SDK only; Velopack packaging targets `net10.0-x64-desktop` and the .NET 10 Desktop Runtime (`ci-build`, `release-pipeline`)
- Documentation and repository structure references updated from .NET 8/C# 12 to .NET 10/C# 14.0

### Fixed

- Removed the redundant explicit `System.Text.Json` package reference exposed by the .NET 10 restore graph

## 0.8.5 — 2026-10-05

### Added

- Named internal test factories (`CreateForTesting(...)`) on `UpdateService` and `ReleaseNotesService`,
  replacing test-only constructor seams (`app-lifecycle`)

### Changed

- `CLIHub.Core` reorganized into subsystem folders (`Agents/`, `Projects/`, `Plugins/`,
  `Configuration/`, `Updates/`, `Infrastructure/`, `Composition/`), with DI registration grouped per
  subsystem; externally observable behavior is unchanged (`refactor-core-boundaries`)
- Configuration ownership split into `AppConfigDocument` (the persisted document), `ProjectState`
  (projects and current project), and `AppPreferences` (preferences), keeping one flat `config.json`,
  the existing JSON contract, and a single atomic write path (`refactor-core-boundaries`)
- Project path-shortening and logo-resolution policies extracted from `ProjectService` into
  `ProjectPathPolicy` and `ProjectLogoResolver` (`refactor-core-boundaries`)
- Process execution split into `IInteractiveProcessRunner` and `IProcessOutputRunner`;
  `IProcessLauncher` retained as a compatibility aggregate contract (`refactor-core-boundaries`)
- Application data paths centralized in a static `AppPaths` class, the single source of truth for the
  `%APPDATA%\CLIHub` layout, consumed by `DirectoryInitializer`, `ConfigService`, `PluginManager`,
  `LogoCacheService`, `PluginSeeder`, logging, and the data-folder action; paths and file names are
  unchanged (`centralize-app-data-paths`)
- Production Core services now expose exactly one public DI constructor: compatibility overloads
  removed from `AgentCommandService`, `AgentVersionService`, `AgentDetectionService`, and
  `ProjectService` (`app-lifecycle`)

### Fixed

- Concurrent version probes for the same agent are coalesced into a single process; caller
  cancellation stops that caller's wait without cancelling the shared probe, and TTL caching is
  retained (`agent-version`)
- Stale version-population results are ignored after the agent list is refreshed, using generation
  and cancellation tracking (`agent-version`)
- Windows command construction is hardened for Windows Terminal, Command Prompt, and PowerShell:
  paths with spaces, quoted arguments, shell metacharacters, executable shims, and `.cmd`/`.bat`
  files are handled correctly for both interactive launches and captured output (`agent-commands`)
- Update-check timeouts cancel or observe the underlying operation and clear the stale available
  version instead of leaving a late task unobserved (`update-checking`)
- Async refresh and update-check paths are bounded by cancellation and generation guards so they
  cannot outlive the state they belong to (`agent-availability-display`, `update-checking`)
- The launch-window agent collection synchronizes by stable plugin ID, preserving `AgentItem`
  identity and the current selection across refreshes (`agent-availability-display`)
- UI fire-and-forget commands route through a shared async error boundary that logs unexpected
  exceptions with operation context and reports a user-safe status message

## 0.8.0 — 2026-10-04

### Changed

- The launch-window update control is now a reusable state-aware view model that keeps update checks, downloads, and restart actions consistent across the window (`update-checking`)

## 0.7.0 — 2026-10-04

### Added

- Single update control in the launch window footer: shows the current version when idle, checks
  for updates on click, downloads an available release, and restarts to apply it; the control
  reflects checks and downloads started from the tray or the What's New window and carries accent
  styling whenever an action is available
- Persistent logo cache: project and agent logos resolve through a key-based cache
  (`project:<id>` / `plugin:<id>`) that loads at startup, updates write-through during runs, and
  saves at shutdown; negative results are cached too, a removed project drops its entry, and a
  manual refresh re-resolves (`logo-cache`)
- `IConfigService.Flush()` drains pending configuration writes synchronously; the service flushes
  on disposal, and the DI container disposes it at shutdown so no change is lost
  (`app-lifecycle`)

### Changed

- Configuration saves no longer write to disk on the calling thread: `ConfigService.Save`
  snapshot-serializes on the caller, and a single debounced background worker (250 ms) writes the
  file atomically, coalescing rapid saves; in-memory `Load` semantics are unchanged
  (`project-management`)
- The launch window syncs the project list by membership diff instead of rebuilding it, and
  `Project` raises property-change notifications for favorite, last-used, and logo, so rows and
  the selection update in place (`project-management`)
- The pane actions menus are composed from a data model instead of hand-built XAML
  (`main-window-layout`)
- Settings agent-probe fields show their default values as watermarks when empty
- GitHub Actions are bumped to node 24 majors (checkout v7, setup-dotnet v6)

### Fixed

- The pane scrollbar renders inside the list's edge instead of overlapping row content
- Shortened project paths are budgeted against the row's live text column, so they fit without
  being cut off
- Pane rows keep equal insets on the divider side

## 0.6.0 — 2026-10-03

### Added

- CI and release pipeline: build-and-test workflow on every pull request and push to `master`, and a
  tag-driven release workflow that tests, packages with `vpk` (framework-dependent win-x64 with a
  .NET 8 Desktop Runtime bootstrap), and publishes to GitHub Releases (`ci-build`, `release-pipeline`)
- Release runbook and pipeline documentation (`docs/releasing.md`, `docs/architecture.md → Packaging & CI/CD`)
- Downloading and applying updates from the tray: a one-click download-and-restart action in the tray menu
  and the What's New window, with a downloading state in the menu, completion and failure notifications, and
  a guard against concurrent downloads (`update-checking`)
- Dark theme for the Settings window: drawn chrome like the launch window (no OS title bar, header drag,
  Escape to close, DWM-rounded corners), an uppercase section rhythm with hint lines, segmented runtime and
  path-display selectors, a chip-styled hotkey capture field, themed inputs, checkboxes, and footer — with
  the window's height following the launch window's (`settings-theme`)
- Themed tooltips in the launch window, Settings, and What's New, sharing one slim dark style
  (`launch-window-theme`)

### Changed

- The update feed points at the real repository (`trustsoft/clihub`) instead of the `your-org/clihub`
  placeholder, so installed builds resolve the live GitHub Releases source (`update-checking`)
- The slim dark scrollbar is now an application-level style (it must live there to reach the scrollbars
  inside templates) and shrank to 4px, so the launch window lists, Settings, and What's New scroll
  consistently
- The Settings window opens at its 420px minimum width with a 30px probe input pair sized to the
  "Timeout, seconds" caption, auto-width checkboxes, an auto-width hotkey field with equal 8px insets, and
  segment groups with 2px gaps
- The Settings footer's brand and version chip reuse the launch window's styles

### Fixed

- The What's New window no longer draws the native frame of a resizable window around its own border and
  drops the top highlight, so its chrome matches the launch window's

## 0.5.0 — 2026-09-30

### Added

- .NET 8 WPF system-tray application scaffold: a dependency-injection container, Serilog file logging
  with 7-day retention and a configurable level, and the core service interfaces (`app-lifecycle`, `logging`)
- Single-instance enforcement via a named mutex, with a named pipe so a second launch activates the running
  instance and exits (`app-lifecycle`)
- Project management: folders tracked with an auto-detected logo, a favorite flag and last-used time, a
  recent-projects list, and a current project that provides launch context (`project-management`)
- Plugin descriptors (`%APPDATA%\CLIHub\plugins\<id>\plugin.json`) describing an agent's commands and
  detection markers, seeded on first run for six built-in agents — OpenCode, Pi, Cline CLI, GitHub Copilot,
  OpenClaude, and Qwen Code — together with their logos (`plugin-seeding`)
- Agent command set — launch, resume, version, update, and init — executed in the current project's folder,
  interactively through Windows Terminal, Command Prompt, or PowerShell, with output captured for the
  non-interactive commands (`agent-commands`)
- Agent availability detection: installed on the host and usable in a given project, with TTL-cached
  results (`agent-detection`)
- Per-agent version display, filled in asynchronously after the list renders, with an explicit cache
  invalidation on refresh (`agent-version`)
- Availability display: agents unavailable in the current project are dimmed, optionally hidden
  (`agent-availability-display`)
- Global hotkey (default `Ctrl+Shift+A`) registered with Windows, configurable, and re-registered at runtime
  without a restart; the parser accepts letters, digits, `F1`–`F24`, Space, and named keys such as Enter,
  Tab, and the arrows (`hotkey-support`)
- Update checking through Velopack: an asynchronous startup check, a manual check, a tray notification
  carrying the new version, and the current version shown in the window (`update-checking`)
- Settings window: default runtime, global-hotkey capture, agents-probe TTL and timeout, the startup update
  check, and the project path display style — all applied without restarting (`preferences-ui`)
- Start with Windows through a per-user Run registration, plus a "show window on startup" preference so the
  application can start in the tray (`app-lifecycle`)
- Resizable Projects and AI Agents panes with a draggable divider and an aligned pane layout
  (`main-window-layout`)
- Dark two-pane launch window, rebuilt as a chromeless popup shell: DWM-rounded corners, always on top,
  hidden when it loses focus unless pinned, Escape to hide, opening on the pointer's monitor, pane Actions
  menus, footer actions, and a version chip (`main-window-layout`, `launch-window-theme`)
- Remove-project action in the launch window, behind a confirmation that never deletes files

### Changed

- **BREAKING**: the plugin descriptor schema changed — the flat `commands` array became a `commands` object
  (`launch`, `resume`, `version`, `update`, `init`) plus a `detection` block. Existing descriptors must be
  updated (`agent-commands`)
- The `terminalExecutable` path preference is superseded by a three-way `defaultRuntime` (`wt` / `cmd` / `ps`);
  the old value is migrated once and kept for backward compatibility (`preferences-ui`)
- `user32.dll` declarations moved to `src/CLIHub/Interop/User32.cs` and converted to source-generated
  P/Invoke; behavior is unchanged
- `using` directives moved inside file-scoped namespaces, with an editorconfig rule that flags regressions
- The pre-redesign window is retained for reference but is no longer wired; the launch window is the
  application window (`main-window-layout`)

### Fixed

- Refreshing the agent list now invalidates the detection cache, so availability is re-evaluated instead of
  being served from a stale result (`agent-detection`)
- The hotkey parser accepts Space and additional named keys instead of falling back to the default
  combination (`hotkey-support`)
