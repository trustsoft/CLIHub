# CLIHub Architecture

## Solution Structure

CLIHub is a three-project solution: `CLIHub.Core` (logic) and `CLIHub` (WPF UI) under `src/`, plus `CLIHub.Tests` under `tests/`. The full directory tree, folder inventory, and build output live in [repo-structure.md](repo-structure.md). Project responsibilities and dependency rules follow.

## Project Responsibilities

### CLIHub.Core

**Purpose:** Platform-agnostic business logic and services.

**Models:** `Plugin`, `PluginCommand`, `AgentCommands`, `AgentCommandKind`, `AgentDetection`, `AgentCommandResult`, `ProcessCaptureResult`, `Project`, `RuntimeKind`/`RuntimeKinds`, `AppConfig`, `AppPreferences`, `UpdateCheckResult`/`UpdateStatus`, `UpdateDownloadResult`/`UpdateDownloadStatus`, `ReleaseNote`.

**Services:**
- `ConfigService` — JSON configuration load/save with atomic writes
- `ProjectService` — project tracking (current, recent, favorites, logo resolution)
- `PluginManager` — plugin discovery/validation from `plugins\<id>\plugin.json`
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

**Interfaces:** `IConfigService`, `IProjectService`, `IPluginManager`, `IPluginSeeder`, `IAgentCommandService`, `IAgentDetectionService`, `IAgentVersionService`, `IProcessLauncher`, `IUpdateService`, `IReleaseNotesService`, `IStartupService`.

**Utilities:** `HotkeyParser`/`HotkeyModifiers`/`HotkeyDefinition`, `LoggingSetup`/`LogLevelParser`/`PreferenceReader`, `MiddleEllipsisFormatter` and `PathLeftTrimFormatter` (path shortening for display, selected by `PathDisplayStyle`/`PathDisplayStyles`), `ServiceCollectionExtensions` (`AddClIHubCoreServices`).

**Dependencies:** .NET 8, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging`, `Serilog`, `System.Text.Json`, `Velopack`. No WPF dependency.

**Target:** `net8.0`.

### CLIHub

**Purpose:** WPF user interface and Windows-specific integration.

**Contains:** app entry/DI wiring (`Program` with the Velopack bootstrap, `App.xaml(.cs)`, `ServiceRegistration`), `TrayIconController` (H.NotifyIcon.Wpf), `ISettingsLauncher`/`SettingsLauncher` (owns the single Settings window instance), `PromptState` (keeps the popup visible while a prompt is open), `LaunchWindow` + `LaunchWindowViewModel` (the chromeless popup shell: no OS chrome, always on top, hides when it loses focus unless pinned, Escape hides it, centred on the pointer's monitor on every show; shown from the tray, the hotkey, a second-instance activation and startup), `MainWindow` (retained from the pre-redesign scaffold; kept for reference, no longer registered or constructed), `AgentItem`, `GlobalHotkeyService`, `PathToImageConverter`, `PathDisplayConverter`, `Themes/` (`LaunchTheme.xaml` palette — the single place for colors; `Sizing.xaml` metrics and tokens such as the icon font family; `Controls.xaml` shared keyed styles — button templates (`IconChipButton`, `QuietChipButton` and their derivatives), drawn-window chrome (`WindowHeader`, `WindowTitle`, `WindowCloseButton`), common typography (`SectionLabel`, `BrandText`, `VersionChip*`) and the `DarkToolTip` base style; `DarkScrollBar.xaml` shared dark-control style; `SettingsStyles.xaml`, `LaunchWindowStyles.xaml` and `WhatsNewStyles.xaml` window-scoped styles that merge `Controls.xaml`, which brings the palette and sizing tokens transitively (StaticResource inside a dictionary resolves only against that dictionary's own merged dictionaries). `App.xaml` hosts the implicit dark ToolTip style — tooltips, like scrollbars, live outside the window visual tree, so only an application-level implicit style reaches them — and the shared converter instances (`PathToImage`, `PathDisplay`, `BoolToVisibility`), `Interop/User32` (source-generated `user32.dll` P/Invoke), `Interop/DwmApi` (window corner rounding), `Interop/WindowPositioner` (pointer-monitor placement), and the Settings window (`SettingsWindow` + `SettingsViewModel`, MVVM, dark drawn chrome like the What's New window: drawn header, segmented runtime/path selectors, chip-styled hotkey capture field, themed footer; owned by the launch window so it stays visible above it, its height following the launch window's) with `IPreferenceApplier`/`PreferenceApplier`. The What's New window (`WhatsNewWindow` + `WhatsNewViewModel`, MVVM) shows the embedded release notes newest-first and is reached from the tray or shown once after an upgrade (`IReleaseNotesLauncher`/`ReleaseNotesLauncher`, single instance, owned by the launch window like Settings); when an update is available its header offers the same download-and-restart action as the tray menu, sharing the update state. It draws its own dark chrome — no OS title bar, a header that drags the window, and a close button — but stays an ordinary resizable window rather than a popup: not always on top, no hide-on-focus-loss. Startup wires the configured runtime, refreshes the start-with-Windows registration, creates the tray icon, shows the launch window when the preference asks for it, registers the global hotkey, shows the What's New window once when the running version differs from the recorded one (recording a version that has no notes — and a first run — without showing anything), and (when enabled) runs a background update check that raises a tray notification when an update is available. Window logic lives in `ViewModels/` (MVVM).

**Dependencies:** CLIHub.Core, WPF, H.NotifyIcon.Wpf, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Serilog.

**Target:** `net8.0-windows`.

### CLIHub.Tests

**Purpose:** unit tests (xUnit) for Core behavior.

**Contains:** `ProjectServiceTests`, `PluginManagerTests`, `PluginSeederTests`, `AgentCommandServiceTests`, `AgentDetectionServiceTests`, `AgentListComposerTests`, `AgentVersionServiceTests`, `ProcessLauncherTests`, `UpdateServiceTests`, `ReleaseNotesServiceTests`, `LoggingSetupTests`, `MiddleEllipsisFormatterTests`, `PathLeftTrimFormatterTests`, `PathDisplayStyleTests`, `HotkeyParserTests`, `SingleInstanceGuardTests`, `StartupServiceTests`, `ServiceCollectionExtensionsTests`, plus `FakeConfigService`/`FakeProcessLauncher`/`FakeStartupRegistry`/`FakeTimeProvider`.

**Dependencies:** CLIHub.Core, xUnit, Microsoft.NET.Test.Sdk, Microsoft.Extensions.DependencyInjection (for the composition test), coverlet.

**Target:** `net8.0`. UI/tray/terminal behavior is verified manually.

## Dependency Flow

```
CLIHub.Tests ──> CLIHub.Core <── CLIHub
                                   │
                                   ├─> System Tray (H.NotifyIcon.Wpf)
                                   ├─> Windows Terminal (wt.exe)
                                   └─> Global hotkey (RegisterHotKey)
```

**Rules:**
- `CLIHub.Core` has NO dependency on `CLIHub` (UI)
- `CLIHub.Tests` references `CLIHub.Core` only (not the UI)
- Business logic lives in `CLIHub.Core` for testability
- `CLIHub` is a thin presentation layer over Core services

## Capabilities (per `openspec/specs/`)

`app-lifecycle`, `logging`, `project-management`, `plugin-seeding`, `agent-commands`, `agent-detection`, `agent-version`, `agent-availability-display`, `hotkey-support`, `update-checking`, `main-window-layout`, `preferences-ui`, `release-notes`, `release-notes-display`, `ci-build`, `release-pipeline`. Each spec defines observable behavior; see the corresponding spec for requirements.

## Technology Stack

### Core Technologies
- **.NET 8 LTS** — long-term support
- **C# 12** with nullable reference types and implicit usings enabled
- **WPF** — Windows Presentation Foundation for UI

### Libraries
- **Microsoft.Extensions.DependencyInjection** — service container
- **Serilog** with file sink (+ `Serilog.Extensions.Logging`) — structured logging, bridged into `Microsoft.Extensions.Logging`
- **H.NotifyIcon.Wpf** — system tray icon
- **System.Text.Json** — JSON serialization with camelCase policy
- **Velopack** — application update checking, downloading, and packaging (GitHub Releases source); packaging runs in CI, see [Packaging & CI/CD](#packaging-cicd)

### External Integration
- **Windows Terminal** (`wt.exe`) — spawns CLI tool sessions
- **Named mutex** (`Local\CLIHub.SingleInstance`) + **named pipe** (`CLIHub.SingleInstance`) — single instance enforcement and activation
- **user32.dll** (`RegisterHotKey`/`UnregisterHotKey`) — global hotkey registration via source-generated `[LibraryImport]` in `src/CLIHub/Interop/User32.cs`
- **Velopack** — update checks, package downloads, and apply-and-restart against GitHub Releases; the current version comes from the Velopack locator (falling back to the assembly informational version)
- **Registry (HKCU Run)** — the per-user `Software\Microsoft\Windows\CurrentVersion\Run` value `CLIHub` controls start-with-Windows

## Plugin Descriptor Format

Each plugin lives in `%APPDATA%\CLIHub\plugins\<id>\` with `plugin.json` and an optional `logo.png`:

```jsonc
{
  "id": "opencode",
  "name": "OpenCode",
  "description": "AI coding agent CLI",
  "commands": {
    "launch":  { "executable": "opencode" },
    "resume":  { "executable": "opencode", "arguments": "--continue" },
    "version": { "executable": "opencode", "arguments": "--version" },
    "update":  { "executable": "opencode", "arguments": "upgrade" }
  },
  "detection": {
    "systemPaths": [ "%USERPROFILE%\\.opencode" ],
    "projectIndicators": [ ".opencode", "openspec" ]
  }
}
```

- `commands` is a named set; only `launch` is required.
- `detection.systemPaths` are host install markers (env vars expanded); `detection.projectIndicators` are project-relative markers.
- Built-in descriptors for OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code are embedded and seeded on first run.

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

Two GitHub Actions workflows, both on `windows-latest` (WPF does not build on Linux):

- **`ci.yml`** — every pull request and every push to `master`: `dotnet build CLIHub.sln -c Release`, then `dotnet test`.
- **`release.yml`** — triggered by pushing a tag `v*`:
  1. a `test` job builds and tests the solution as a gate;
  2. a `release` job (`permissions: contents: write`) derives the version from the tag (leading `v` stripped; the tag is the source of truth — `Directory.Build.props` stays the development default), extracts the released version's section from `RELEASE-NOTES.md` (fails the job when the section is missing), and publishes framework-dependent `win-x64` with `-p:Version=<tag version>`;
  3. `vpk pack` (the Velopack CLI tool, pinned to the exact Velopack library version the app links) produces the setup, portable, and delta assets with `--runtime win-x64 --framework net8.0-x64-desktop` — the installer offers the .NET 8 Desktop Runtime when it is missing — and the notes file;
  4. `vpk upload github` publishes everything to the repository's GitHub Releases as a published release on the pushed tag (`--merge true`, so re-running for the same tag updates the release). `GITHUB_TOKEN` authenticates the upload; the extracted notes section becomes the release body and the updater's notes.

- **Update channel:** stable only (the default `win` channel). Prerelease channels are not wired.
- **Signing:** the packages are unsigned (no certificate yet); SmartScreen may warn on first install.
- **User prerequisite:** the .NET 8 Desktop Runtime (x64), unless the installer's runtime bootstrap installs it.
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

- **Format:** JSON with camelCase property names
- **Location:** `%APPDATA%\CLIHub\config.json`
- **Atomic writes:** write to a `.tmp` file, then rename (temp-file-then-rename)
- **Schema:** `AppConfig` with `projects`, `preferences`, and `currentProjectId`. `AppPreferences` includes `hotkey` (`Ctrl+Shift+A` by default), `defaultRuntime` (`wt`/`cmd`/`ps`), `logLevel`, `showOnlyProjectAgents`, `agentProbeTtlMinutes`, `agentProbeTimeoutSeconds`, `checkForUpdatesOnStartup`, `startWithWindows`, `showWindowOnStartup`, `pinLaunchWindow`, `pathDisplayStyle` (`leftTrim` by default, `middleEllipsis` as the alternative; unrecognized values fall back to the default), and `lastSeenReleaseNotesVersion` (the version whose release notes were last shown — written by the application, not editable in Settings; missing or null means the notes have never been shown, which is treated as a first run without opening the What's New window) (all optional; missing values fall back to defaults); the legacy `terminalExecutable` is superseded by `defaultRuntime`
- **Forward compatibility:** missing fields deserialize to defaults; unknown fields are ignored. There is no explicit schema-version field yet (adding one is deferred).

## Logging Strategy

- **Framework:** Serilog with file sink, bridged into `Microsoft.Extensions.Logging` (`ILogger<T>` injected via DI)
- **Location:** `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log`
- **Rotation:** daily files, 7-file retention
- **Levels:** configurable via `AppPreferences.LogLevel` (default `Information`); `Microsoft.*` and `System.*` overridden to `Warning`
- **Format:** `[timestamp level] message` with exception details
- **Shutdown:** `Log.CloseAndFlush()` on exit

## Error Handling Strategy

### Configuration Errors

- **Missing config.json:** create defaults on first run
- **Corrupted/invalid config.json:** log the error and fall back to in-memory defaults (the file is rewritten on the next save)

### Plugin Errors

- **Invalid plugin.json:** skip during discovery, log a warning (with the missing field)
- **Duplicate plugin IDs:** load the first, skip duplicates, log a warning
- **Missing logo:** fall back to the default project logo

### Process / Agent Errors

- **Missing executable / permission denied:** log the error, return a failed result, surface a message in the UI
- **Non-zero exit for `version`:** report unknown and log

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

### Namespace Structure
- `CLIHub.Core.Models`, `CLIHub.Core.Services`, `CLIHub.Core.Interfaces`, `CLIHub.Core.Hotkeys`, `CLIHub.Core.Logging`, `CLIHub.Core.Formatting`
- `CLIHub` (App, controllers), `CLIHub.Views`, `CLIHub.Hotkeys`, `CLIHub.Interop`, `CLIHub.Converters`, `CLIHub.ViewModels`
- `CLIHub.Themes` (XAML resource dictionaries; the only code is `IconGlyphs`, the compile-time checked Segoe MDL2 glyph constants referenced from XAML via `{x:Static themes:IconGlyphs.Name}`)

### Resource Organization
- Application icon: `src/CLIHub/app.ico` (embedded; also the exe icon)
- Default project logo: `src/CLIHub/default-project.png` (copied to output)
- Desktop app icon sources: `assets/` at the repository root (not part of the build)
- Embedded seed descriptors/logos: `src/CLIHub.Core/SeedPlugins/`

### XAML Themes and Styles Recipe

Layering: `LaunchTheme.xaml` (palette — the only place with colors) + `Sizing.xaml` (metrics, font and radius tokens) → `Controls.xaml` (shared keyed styles; merges palette and Sizing itself) → per-window dictionaries (`LaunchWindowStyles.xaml`, `SettingsStyles.xaml`, `WhatsNewStyles.xaml`) that merge only `Controls.xaml`. App.xaml hosts the app-level pieces: the palette (for window attributes), `Controls.xaml`, the implicit `ScrollBar` and `ToolTip` styles, and the shared converters.

To add a window:
1. Create `Themes/<Window>Styles.xaml` merging `/Themes/Controls.xaml` (this brings the palette and sizing tokens transitively — StaticResource inside a dictionary resolves only against that dictionary's own merged dictionaries).
2. Define keyed styles only (no implicit styles — they would not reach popups anyway); build on the shared bases (`IconChipButton`, `QuietChipButton`, `WindowHeader`, `WindowTitle`) via `BasedOn`.
3. Merge it from the window's `Window.Resources`; reference colors exclusively through palette brushes, metrics through `Sizing.*`, glyphs through `IconGlyphs`.
4. Add tooltips as plain `ToolTip="..."` attributes — the app-level implicit style themes them automatically.

To create a new palette (compile-time theme choice, applied at startup):
1. Copy `LaunchTheme.xaml` to a new file and change the colors.
2. Point the palette merges in `App.xaml` and `Controls.xaml` at the new file.
Runtime re-theming without restart would require brushes referenced via `DynamicResource` — deliberately deferred until a second theme ships.

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
