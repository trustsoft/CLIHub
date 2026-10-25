# Changelog

The technical record of what changed in CLIHub: what, why, and which capability spec it belongs to.
User-facing notes for the same releases live in [RELEASE-NOTES.md](RELEASE-NOTES.md); both files use the
same version heading (`## <version> — <date>`) so a release lines up across them.

Groupings: `Added`, `Changed`, `Fixed`, `Removed`. A change that breaks existing behavior or
configuration is marked `**BREAKING**`. Capability names in parentheses refer to `openspec/specs/<name>`;
internal-refactor names refer to `openspec/changes/archive/<name>`.

## 1.0.1 — 2026-10-24

### Added

- While the stored theme is System, a change of the Windows apps-use-light setting re-resolves the effective theme and reapplies the matching palette to every open window through the shared `PaletteSwitcher`; the follower is owned by the first-instance session, disposed at shutdown, and fixed themes ignore personalization changes; System still never resolves to Blue (`application-theme`)

### Changed

- The CLI Agents page renders cards with declarative Update availability and populates the blocked state in a fire-and-forget pass off the UI thread, so Settings opens without the running-agent enumeration; until the pass lands a blocked card can show Update as available, choosing it is still blocked by the execution-time gate, a re-load supersedes the in-flight pass, and closing the window cancels it (`agent-ui`)
- The agent version-population loops of the launch pane and the CLI Agents page share one shape — generation counter, surface-owned cancellation on supersession and exit, parallel probes with generation-guarded writes, and unexpected failures logged through Serilog instead of `Debug.WriteLine`; the version display is unchanged (`consolidate-version-population`)
- Hygiene sweep: removed the registered-but-unused `RuntimeSelector` with its `RuntimeType.WindowsTerminal` producer member and the unused `AgentProcessMatcher` logger; `.gitnexus/` is ignored; the Settings Projects page reuses `ProjectPathPolicy` for duplicate-folder detection (an already-registered drive/UNC root now reports the duplicate error instead of being silently accepted); `ProjectService` mutations return detached copies (`hygiene-sweep`)
- The CLI Agents page shows the probe settings above the agent list (page order MANAGEMENT → PROBE → AGENTS); layout-only reorder, no behavior change

## 1.0.0 — 2026-10-23

### Added

- A third palette, Blue, behind a descriptor-driven theme registry: `AppTheme`, `ThemeDescriptor`, and `ThemeCatalog` resolve Dark, Light, Blue, and System without hardcoded branches, and the palette parity test enumerates every registered palette (`theme-registry`)
- Live theme switching: a Settings save applies the resolved palette to every open window through `PaletteSwitcher`, so the theme no longer waits for the next start (`live-theme-switch`)
- The accent color is application-scoped and independent of the theme: palettes carry chrome and surface brushes only, `AccentApplier` writes the accent into `Application.Resources`, and a theme switch never changes the accent (`accent-independent-of-theme`)
- A CI and release gate that fails when the `Directory.Build.props` version is older than the highest stable `v*` tag, with full tag history checked out in both workflows (`keep-dev-version-current`)

### Changed

- Theme and Settings XAML resources are layered for extension: `Sizing`/`Controls`/`ThemePickerData`/`SettingsControls`/`SettingsStyles` merge once, shared card and badge templates replace duplicated page markup, and adding a palette or page no longer duplicates trigger chains (`theme-xaml-structure`, `tidy-theme-resources`)
- The launch and Settings footers share `Sizing.Footer.*` insets: the brand block starts 12 DIP from the left, the action block ends 8 DIP from the right, and the Settings version chip is optically lowered 2 DIP against the brand label (`settings-theme`, `main-window-layout`)

### Fixed

- Opening Settings no longer stalls for many seconds while the CLI Agents page loads: the running-agent check that gates each card's Update action now performs a single process enumeration per load, and the process inspector pre-filters by the registered launch executable names before reading process details — no change to card behavior (`fix-settings-open-delay`)

## 0.9.9 — 2026-10-20

### Added

- Descriptor-declared update availability checking (`agent-update-detection`): an optional `updateCheck` block in plugin.json selects the npm or GitHub Releases channel, the latest version is compared numerically against the probed installed version, and the agents page shows an advisory `installed → latest` chip; results are cached per agent with the shared probe TTL, invalidated per agent on probes and update starts, and never gate the Update action; unknown source kinds are ignored at use time
- Built-in descriptors declare their distribution channels: npm for opencode, cline, pi, openclaude, and qwen-code; GitHub Releases for GitHub Copilot CLI (`plugin-seeding`)
- A runtime executable locator resolves Git Bash and PowerShell 7 to absolute paths — registry InstallPath, well-known locations, then PATH (outside the Windows directory for bash) — and Terminal Profiles marks Git Bash or PowerShell 7 as not installed when they cannot be resolved (`agent-commands`, `preferences-ui`)
- A backlog for parked findings with evidence, remedies, and revisit triggers (`docs/backlog.md`)

### Changed

- Application registration is grouped into feature modules (`modularize-di-registration`)
- The logo pipeline is unified behind `LogoOptions` and domain resolvers (`unify-logo-pipeline`)
- Projects are read through enriched `ProjectModel` rows with detached reads (`introduce-project-model`)
- The plugin catalog hands out detached copies, the descriptor model carries the cloned `updateCheck` block, and an agent's update action is gated only by that agent's own running processes (`harden-agent-plugin-boundaries`, `agent-ui`)
- Probe and detection caches are invalidated per agent instead of clearing every agent's entries, and unreachable runtime command-line matching was removed from the process matcher (`harden-probe-caching-and-process-matching`, `agent-ui`)

### Fixed

- The Git Bash runtime no longer resolves to the WSL launcher or an arbitrary PATH `bash.exe`: launches use the resolved Git for Windows executable, and an unresolvable Git Bash or PowerShell 7 runtime fails with a logged lookup-order error (`agent-commands`)

## 0.9.8 — 2026-10-19

### Added

- Resume command for GitHub Copilot (`copilot --continue`) and Qwen Code (`qwen --continue`), and Update command for Cline CLI (`cline update`) and OpenClaude (`openclaude update`) in the built-in descriptors (`plugin-seeding`)
- Per-agent update blocking: an agent's Update action is blocked only while processes of that same agent are running, so other running agents no longer prevent an update (`agent-ui`, `preferences-ui`)
- Design proposal for agent update availability checking against npm and GitHub Releases (`agent-update-availability`); later implemented as `agent-update-detection` (`openspec/changes/archive/2026-10-20-agent-update-detection/`)

### Changed

- Cline resume stays intentionally omitted from the built-in descriptor: the CLI resumes only by explicit session id via `cline --id` (`plugin-seeding`)
- The update-blocked status message names the single running agent instead of a list of all running agents (`agent-ui`)

### Fixed

- Launching an agent starts it in the selected project folder: the working directory is now applied to the terminal process for every supported runtime (`agent-commands`)
- Interactive agent commands no longer fail in PowerShell and PowerShell 7 with a doubled call operator (`& &`) when built from the descriptor command line (`agent-commands`)
- Arguments of PATH-resolved agents installed as script shims (npm-style) no longer receive the PowerShell stop-parsing token, which broke resume and update commands with arguments (`agent-commands`)

## 0.9.7 — 2026-10-19

### Added

- Settings Projects page with the full registry, manual and discovered provenance, missing-folder flags, and manual add and remove (`preferences-ui`, `project-management`)
- Projects page summary cards for total, manual, and discovered projects, including folder health (`settings-theme`)
- CLI Agents summary cards for installed agents, the default agent, and the effective probe policy (`preferences-ui`, `settings-theme`)
- Agent card compaction with an installation chip, a supported-commands line, and icon actions (`agent-ui`)
- Default-agent gating on detection and a re-detect action in the agents list header (`preferences-ui`)
- Per-root scan depth and discovery mode with row chips, a per-root scan action, and a per-row discovered-repository count (`configuration-snapshot`, `preferences-ui`, `project-scanning`)
- Live scanning edits on Project Discovery: each scan setting is validated, persisted atomically, applied to the scanner, and rescanned in the background (`preferences-ui`)
- Scan coverage (folders examined by the last pass) and the discovered/manual split on the summary cards (`project-scanning`, `settings-theme`)
- `NumericField` in `CLIHub.Controls` with step chips, Up/Down keys, and mouse-wheel stepping (`settings-theme`)
- Theme preview cards, Teal, Indigo, and Tangerine accent presets, a retained custom accent, and the renamed Blue preset (`application-theme`, `preferences-ui`)
- Updates page header chips for version, channel, and update status, with the check and release-notes actions in the header (`preferences-ui`)
- Themed toggle switch checkbox style (`settings-theme`)
- `CLIHub.Core.Windows` platform-adapter library for registry startup, the single-instance guard, theme detection, process runners, and agent process inspection (ADR-0006)

### Changed

- Settings page headers are pinned above the scrolling content, and the Project Discovery summary cards moved into that header (`settings-theme`)
- Launcher positioning labels follow the "<monitor> — <placement>" pattern, and the section is called Launcher Positioning (`configuration-snapshot`, `preferences-ui`)
- Projects & Paths renamed to Project Discovery; the internal page key and stored preferences are unchanged (`preferences-ui`, `settings-theme`)
- Default hotkey changed from Ctrl+Shift+A to Ctrl+Shift+Space; explicitly configured hotkeys are not migrated (`hotkey-support`)
- Single-instance guard acquired before data-directory creation, log configuration, and DI construction, so a duplicate launch exits immediately (`app-lifecycle`)
- Code style normalized: this-qualified member access, the C# field keyword, reflowed XML documentation, and removed unused usings; no behavior change (`code-style`)

### Fixed

- Settings window keeps at least its own size instead of matching a shorter launch window
- Pre-release notifications toggle disabled until a beta channel exists
- Path display selector aligned to the trailing edge of its row
- 0.9.6 release files restored after a stale Git index reverted them
- Remaining compiler warnings cleared

### Removed

- Plugin path and detection markers removed from agent cards; the reference data remains in each `plugin.json` (`agent-ui`)

## 0.9.6 — 2026-10-13

### Added

- Sidebar-navigated Settings window with grouped pages, page icons, and card layouts for settings and data pages (`preferences-ui`, `settings-theme`)
- Application theme choice of light, dark, or system, a live accent color, and dynamic palette brush resolution (`application-theme`, `settings-theme`)
- Launch-window popup position preference, and start-minimized replacing show-window-on-startup (`preferences-ui`)
- Terminal runtime selection across PowerShell, PowerShell 7, Command Prompt, and Git Bash (`preferences-ui`)
- Background update auto-download with stable and pre-release notification options (`update-checking`)
- Log level, retention, and log-folder controls on the Logs page (`logging`, `preferences-ui`)
- Watched-directory project scanning with a background watcher (`project-scanning`)
- Agent management on the CLI Agents page (`agent-ui`)
- SpacedPanel layout control and styled combo box shared via CLIHub.Controls (`spaced-panel-layout`)

### Changed

- Settings window chrome restyled with a target-style title bar, page-header divider, and unified spacing tokens (`settings-theme`)
- Telemetry & Logs page renamed to Logs, and the hotkeys page renamed to Hotkeys & Quick Launcher (`preferences-ui`)
- Removed the init agent command (`agent-commands`)
- Core models, configuration migrations, and view models grouped into subsystem folders, with the shell and test layouts mirrored; no externally observable change

### Fixed

- Removed the invalid DynamicResource binding for the scroll thumb brush

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
- Process subsystem refined around cancellation-safe interactive and output runners (`process-subsystem-refinement`)
- Tray split into a state projection and command handlers, with tray actions separated from the host
- Launch window view model decomposed into pane, menu, status-message, and window-action coordinators

### Removed

- The `IProcessLauncher` compatibility aggregate, replaced by the interactive and output process runner contracts (`retire-compatibility-adapters`)

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
