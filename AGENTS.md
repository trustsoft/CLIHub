# CLIHub Agent Instructions

## Project Status

**Active WPF application** with three projects under `src/`: `CLIHub.Core` (logic), `CLIHub` (WPF UI), `CLIHub.Tests` (xUnit). Delivered capabilities (see `openspec/specs/`): agent commands, detection, version display, availability display/filtering, app lifecycle, hotkey support, logging, plugin seeding, project management.

## Critical Structure Requirements

1. **Source code MUST live in `src/` subdirectory** - do not create projects in the repo root
2. **Solution file** - `CLIHub.sln` at repo root with projects under `src/`
3. **Project structure:**
   ```
   CLIHub/                      (repo root)
   ├── CLIHub.sln               (solution file)
   ├── Directory.Build.props    (build output paths)
   ├── src/
   │   ├── CLIHub.Core/         (business logic; no WPF references)
   │   │   ├── Models/          (Plugin, Project, AppConfig, Agent* models)
   │   │   ├── Services/        (config, projects, plugins, agents, logging)
   │   │   ├── Interfaces/      (service contracts)
   │   │   ├── Hotkeys/         (HotkeyParser)
   │   │   ├── Logging/         (LoggingSetup, LogLevelParser)
   │   │   └── SeedPlugins/     (embedded built-in agent descriptors + logos)
   │   ├── CLIHub/              (WPF app; references CLIHub.Core)
   │   │   ├── Windows/         (MainWindow)
   │   │   ├── ViewModels/
   │   │   ├── Hotkeys/         (GlobalHotkeyService)
   │   │   ├── Converters/
   │   │   └── App.xaml(.cs)    (startup, DI, tray)
   │   └── CLIHub.Tests/        (xUnit; references CLIHub.Core only)
   ├── docs/
   ├── openspec/                (specs + archived changes)
   └── artifacts/ + obj/        (build output; git-ignored)
   ```

## Technology Stack (per design.md)

- **.NET 8 LTS** targeting `net8.0-windows`
- **WPF** (not Windows Forms, not WinUI 3)
- **H.NotifyIcon.Wpf** for system tray (not Hardcodet, not WinForms NotifyIcon)
- **Serilog** with file sink (not NLog)
- **Microsoft.Extensions.DependencyInjection** (not Autofac)
- **Windows Terminal integration** via `wt.exe` spawning

## Key Conventions

- **All code, comments, and UI text in English** - even though Russian is allowed for docs
- **Nullable reference types enabled** - use `?` for nullable types
- **JSON config** at `%APPDATA%\CLIHub\config.json` with camelCase serialization
- **Plugin system** uses JSON descriptors in `%APPDATA%\CLIHub\plugins\<plugin-id>/plugin.json`
- **Log location:** `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log` with 7-day retention

## Code Style

- **File-scoped namespaces** (`namespace X;` not block-scoped).
- **Nullable reference types** enabled globally.
- **Implicit usings** enabled — no need for `using System;` etc.
- **Braces** Use curly braces for if statements and loops.
- **Naming**: PascalCase for public members, `_camelCase` for private fields. Test methods use `MethodOrScenario_Condition_ExpectedResult` pattern.
- **Async patterns**: `async Task` / `ValueTask` used extensively; avoid `async void`.
- **Reactive extensions (if needed)**: R3 (`ObservableCollections.R3`, `R3`) used for reactive patterns in ViewModels.

## Build & Run

```powershell
# From repo root
dotnet build CLIHub.sln
dotnet run --project src/CLIHub/CLIHub.csproj
```

## OpenSpec Workflow

No active change. Work is proposed and archived one change at a time.

```bash
# List main specs (durable capabilities)
openspec list --specs

# Propose the next change
openspec new change "<name>"
openspec validate "<name>"

# Implement, then archive (syncs specs)
openspec archive "<name>"
```

Authored specs live in `openspec/specs/`; completed changes in `openspec/changes/archive/`.

## Known Constraints

- **Windows 10/11 only** - no cross-platform
- **Single instance enforcement** via named mutex `Local\CLIHub.SingleInstance` (session-scoped) with named-pipe activation signaling
- **No in-app terminal** - delegate to Windows Terminal
- **Plugins are descriptors only** - no DLL loading, no in-process execution
- **No Windows Forms** - banned, use WPF equivalents

## File System Layout

- **User data:** `%APPDATA%\CLIHub\` with subdirs: `logs/`, `plugins/`, `cache/`
- **Config persistence:** `config.json` with atomic write (temp file → rename)
- **Plugin discovery:** subdirectories under `plugins/`, each with `plugin.json` + optional `logo.png`

## What to Avoid

- Do NOT use Windows Forms components (`System.Windows.Forms.*`)
- Do NOT create WPF projects without a `.sln` file
- Do NOT put source code in repo root instead of `src/`
- Do NOT implement before discussing structure with user
