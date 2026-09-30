# CLIHub Architecture

## Solution Structure

CLIHub is a three-project solution: `CLIHub.Core` (logic) and `CLIHub` (WPF UI) under `src/`, plus `CLIHub.Tests` under `tests/`. The full directory tree, folder inventory, and build output live in [repo-structure.md](repo-structure.md). Project responsibilities and dependency rules follow.

## Project Responsibilities

### CLIHub.Core

**Purpose:** Platform-agnostic business logic and services.

**Models:** `Plugin`, `PluginCommand`, `AgentCommands`, `AgentCommandKind`, `AgentDetection`, `AgentCommandResult`, `ProcessCaptureResult`, `Project`, `RuntimeKind`/`RuntimeKinds`, `AppConfig`, `AppPreferences`, `UpdateCheckResult`/`UpdateStatus`, `ReleaseNote`.

**Services:**
- `ConfigService` — JSON configuration load/save with atomic writes
- `ProjectService` — project tracking (current, recent, favorites, logo resolution)
- `PluginManager` — plugin discovery/validation from `plugins\<id>\plugin.json`
- `PluginSeeder` — first-run seeding of built-in descriptors + logos
- `AgentCommandService` — execute named commands (launch/resume/version/update/init)
- `AgentDetectionService` — host install + per-project availability (file checks, TTL-cached per preference)
- `AgentVersionService` — version lookup with TTL caching and a configurable probe timeout
- `UpdateService` — Velopack update check (GitHub Releases source) and current-version lookup
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

**Contains:** app entry/DI wiring (`Program` with the Velopack bootstrap, `App.xaml(.cs)`, `ServiceRegistration`), `TrayIconController` (H.NotifyIcon.Wpf), `ISettingsLauncher`/`SettingsLauncher` (owns the single Settings window instance), `PromptState` (keeps the popup visible while a prompt is open), `LaunchWindow` + `LaunchWindowViewModel` (the chromeless popup shell: no OS chrome, always on top, hides when it loses focus unless pinned, Escape hides it, centred on the pointer's monitor on every show; shown from the tray, the hotkey, a second-instance activation and startup), `MainWindow` (retained from the pre-redesign scaffold; kept for reference, no longer registered or constructed), `AgentItem`, `GlobalHotkeyService`, `PathToImageConverter`, `PathDisplayConverter`, `Themes/` (`LaunchTheme.xaml` palette, `Sizing.xaml` metrics, `LaunchWindowStyles.xaml` and `WhatsNewStyles.xaml` window-scoped styles), `Interop/User32` (source-generated `user32.dll` P/Invoke), `Interop/DwmApi` (window corner rounding), `Interop/WindowPositioner` (pointer-monitor placement), and the Settings window (`SettingsWindow` + `SettingsViewModel`, MVVM, owned by the launch window so it stays visible above it) with `IPreferenceApplier`/`PreferenceApplier`. The What's New window (`WhatsNewWindow` + `WhatsNewViewModel`, MVVM) shows the embedded release notes newest-first and is reached from the tray or shown once after an upgrade (`IReleaseNotesLauncher`/`ReleaseNotesLauncher`, single instance, owned by the launch window like Settings). It draws its own dark chrome — no OS title bar, a header that drags the window, and a close button — but stays an ordinary resizable window rather than a popup: not always on top, no hide-on-focus-loss. Startup wires the configured runtime, refreshes the start-with-Windows registration, creates the tray icon, shows the launch window when the preference asks for it, registers the global hotkey, shows the What's New window once when the running version differs from the recorded one (recording a version that has no notes — and a first run — without showing anything), and (when enabled) runs a background update check that raises a tray notification when an update is available. Window logic lives in `ViewModels/` (MVVM).

**Dependencies:** CLIHub.Core, WPF, H.NotifyIcon.Wpf, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Serilog.

**Target:** `net8.0-windows`.

### CLIHub.Tests

**Purpose:** unit tests (xUnit) for Core behavior.

**Contains:** `ProjectServiceTests`, `PluginManagerTests`, `PluginSeederTests`, `AgentCommandServiceTests`, `AgentDetectionServiceTests`, `AgentVersionServiceTests`, `ProcessLauncherTests`, `UpdateServiceTests`, `ReleaseNotesServiceTests`, `LoggingSetupTests`, `MiddleEllipsisFormatterTests`, `PathLeftTrimFormatterTests`, `PathDisplayStyleTests`, `HotkeyParserTests`, `SingleInstanceGuardTests`, `StartupServiceTests`, `ServiceCollectionExtensionsTests`, plus `FakeConfigService`/`FakeProcessLauncher`/`FakeStartupRegistry`/`FakeTimeProvider`.

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

`app-lifecycle`, `logging`, `project-management`, `plugin-seeding`, `agent-commands`, `agent-detection`, `agent-version`, `agent-availability-display`, `hotkey-support`, `update-checking`, `main-window-layout`, `preferences-ui`. Each spec defines observable behavior; see the corresponding spec for requirements.

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
- **Velopack** — application update checking and packaging (GitHub Releases source)

### External Integration
- **Windows Terminal** (`wt.exe`) — spawns CLI tool sessions
- **Named mutex** (`Local\CLIHub.SingleInstance`) + **named pipe** (`CLIHub.SingleInstance`) — single instance enforcement and activation
- **user32.dll** (`RegisterHotKey`/`UnregisterHotKey`) — global hotkey registration via source-generated `[LibraryImport]` in `src/CLIHub/Interop/User32.cs`
- **Velopack** — update checks against GitHub Releases; the current version comes from the Velopack locator (falling back to the assembly informational version)
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

**Unit tests (CLIHub.Tests):** project tracking/logo resolution, plugin discovery/validation, seeding, agent command routing, availability detection, version extraction/caching, update check status/version handling, release-notes parsing (ordering, groups, malformed headings, embedded document), process output capture, logging setup/level parsing, path shortening (middle ellipsis, left trim, style parsing), hotkey parsing, single-instance guard, DI composition.

**Manual verification:** system tray behavior, window show/hide and hotkey toggle, the popup shell (chromeless chrome, hide on focus loss, pin, Escape, pointer-monitor placement), agent launch in the selected runtime, seeded logos/versions, availability dimming/filtering, the launch window (dark theme, pane headers with Actions menus, footer actions, version chip, update check, open data folder, project/agent rows, path display style, scrolling), the update-available tray notification, the Settings window (runtime/hotkey/probe/updates/path-display changes applied without restart; Cancel discards), the What's New window (tray entry, notes newest first in the drawn dark chrome, header drag, resize, Escape and the close button), the one-time release-notes display after a version change, and start-with-Windows plus window-visibility-on-startup.

## Conventions

### Code Style

See [AGENTS.md → Code Style](../AGENTS.md#code-style) (file-scoped namespaces, nullable, implicit usings, braces, naming, async, MVVM).

### Namespace Structure
- `CLIHub.Core.Models`, `CLIHub.Core.Services`, `CLIHub.Core.Interfaces`, `CLIHub.Core.Hotkeys`, `CLIHub.Core.Logging`, `CLIHub.Core.Formatting`
- `CLIHub` (App, controllers), `CLIHub.Windows`, `CLIHub.Hotkeys`, `CLIHub.Interop`, `CLIHub.Converters`, `CLIHub.ViewModels`
- `CLIHub.Themes` (XAML resource dictionaries, no code namespace)

### Resource Organization
- Application icon: `src/CLIHub/app.ico` (embedded; also the exe icon)
- Default project logo: `src/CLIHub/default-project.png` (copied to output)
- Desktop app icon sources: `assets/` at the repository root (not part of the build)
- Embedded seed descriptors/logos: `src/CLIHub.Core/SeedPlugins/`

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
- **Not installed:** when the app does not run from a Velopack install, checks are skipped and reported as `NotInstalled`
- **Scope:** the current implementation only *checks* and notifies (tray notification / status bar); applying an update is deferred to the packaging change
