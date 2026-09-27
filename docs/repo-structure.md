# Repository Structure

```
.
├── src/                     # Source code (three projects)
│   ├── CLIHub.Core/         # Business logic, services, models (no WPF dependencies)
│   ├── CLIHub/              # WPF application, UI, system tray, hotkey
│   └── CLIHub.Tests/        # Unit tests (xUnit)
├── docs/                    # Project documentation
├── assets/                  # Application icon source files (not in the build)
├── openspec/                # Specs (openspec/specs) + archived changes
├── .opencode/               # OpenCode CLI configuration and skills
├── .idea/                   # JetBrains Rider project settings
├── .vs/                     # Visual Studio settings (git-ignored)
├── .git/                    # Git metadata
├── artifacts/               # Build output (git-ignored)
├── obj/                     # Intermediate build artifacts (git-ignored)
├── CLIHub.sln               # Visual Studio solution file
├── Directory.Build.props    # MSBuild properties (build output paths)
├── .gitignore               # Git exclusions
└── AGENTS.md                # OpenCode agent instructions
```

## Source Code

**`src/`** — three-project solution architecture:

- **`CLIHub.Core/`** — core business logic (class library, `net8.0`; no WPF)
  - `Models/` — `Plugin`, `PluginCommand`, `AgentCommands`, `AgentCommandKind`, `AgentDetection`, `AgentCommandResult`, `ProcessCaptureResult`, `Project`, `AppConfig`, `AppPreferences`
  - `Services/` — `ConfigService`, `ProjectService`, `PluginManager`, `PluginSeeder`, `AgentCommandService`, `AgentDetectionService`, `AgentVersionService`, `ProcessLauncher`, `SingleInstanceGuard`, `DirectoryInitializer`
  - `Interfaces/` — `IConfigService`, `IProjectService`, `IPluginManager`, `IPluginSeeder`, `IAgentCommandService`, `IAgentDetectionService`, `IAgentVersionService`, `IProcessLauncher`
  - `Hotkeys/` — `HotkeyParser`, `HotkeyModifiers`, `HotkeyDefinition`
  - `Logging/` — `LoggingSetup`, `LogLevelParser`, `PreferenceReader`
  - `SeedPlugins/` — embedded built-in descriptors + logos (opencode, pi, cline-cli, github-copilot, openclaude, qwen-code)
  - `ServiceCollectionExtensions` — `AddClIHubCoreServices`
  - Dependencies: Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging, Serilog, System.Text.Json

- **`CLIHub/`** — WPF application (`net8.0-windows`)
  - `App.xaml(.cs)` — startup: single instance, AppData init, logging, DI container
  - `ServiceRegistration.cs` — DI registrations (core + guard, tray, window)
  - `TrayIconController.cs` — system tray icon and context menu
  - `Windows/` — `MainWindow`, `AgentItem`
  - `Hotkeys/` — `GlobalHotkeyService` (Win32 `RegisterHotKey`, `HwndSource` hook)
  - `Converters/` — `PathToImageConverter`
  - `ViewModels/` — MVVM view models (target for UI logic)
  - `app.ico`, `default-project.png`
  - Dependencies: CLIHub.Core, H.NotifyIcon.Wpf, Microsoft.Extensions.DependencyInjection, Serilog

- **`CLIHub.Tests/`** — unit tests (`net8.0`, xUnit)
  - `Services/` — service tests and fakes (`FakeConfigService`, `FakeProcessLauncher`)
  - `Hotkeys/`, `Logging/`, `Models/`
  - Dependencies: xUnit, Microsoft.NET.Test.Sdk, Microsoft.Extensions.DependencyInjection, coverlet
  - References CLIHub.Core only (not the UI)

See [architecture.md](architecture.md) for design decisions and the plugin descriptor format.

## Documentation

**`docs/`** — project documentation (markdown):

- `vision.md` — vision, goals, audience, capability roadmap
- `architecture.md` — solution architecture, plugin format, technology decisions, conventions
- `repo-structure.md` — this file

## Assets

**`assets/`** — application icon source files (design-tool output): `appIcon/DS4.1F/` and `appIcon/GLM5.3F/` with PNG sizes and `.ico` files. Used as source material; the final `app.ico` is copied into `src/CLIHub/`, and agent logos are embedded under `src/CLIHub.Core/SeedPlugins/`. Not included in the build.

## Planning Artifacts

**`openspec/`** — spec-driven workflow via the OpenSpec CLI:

- `config.yaml` — project configuration and compressed grounding context
- `specs/` — durable capability specs (the source of truth for behavior)
- `changes/archive/` — completed changes (proposals, design, deltas, tasks)

See `openspec/config.yaml` for project context and [OpenSpec documentation](https://github.com/Fission-AI/OpenSpec) for workflow details.

## Tooling Configuration

**`.opencode/`** — OpenCode CLI configuration, skills, and commands.
**`.idea/`**, **`.vs/`** — IDE settings (user-specific, not committed).
**`.git/`** — Git metadata.

## Solution Files

**`CLIHub.sln`** — solution at the repository root, referencing the three projects under `src/`.

**`Directory.Build.props`** — MSBuild properties configuring build output for all projects:
- `BaseOutputPath` → `artifacts/` (compiled binaries)
- `BaseIntermediateOutputPath` → `obj/` (intermediate files)

## Build Output (git-ignored)

- **`artifacts/`** — final binaries per project/configuration, e.g. `artifacts/CLIHub/Debug/net8.0-windows/CLIHub.exe`
- **`obj/`** — intermediate build files, e.g. `obj/CLIHub/Debug/net8.0-windows/`
