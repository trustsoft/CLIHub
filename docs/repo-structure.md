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

**`src/`** — three projects. Responsibilities, dependency rules, and the plugin descriptor format are in [architecture.md](architecture.md); the folder inventory is below.

- **`CLIHub.Core/`** (`net8.0`, no WPF) — folders: `Models/`, `Services/`, `Interfaces/`, `Hotkeys/`, `Logging/`, `SeedPlugins/` (embedded built-in agent descriptors + logos)
- **`CLIHub/`** (`net8.0-windows`) — folders: `Windows/`, `Hotkeys/`, `Converters/`, `Interop/`, `ViewModels/`; files: `App.xaml(.cs)`, `ServiceRegistration.cs`, `TrayIconController.cs`, `app.ico`, `default-project.png`
- **`CLIHub.Tests/`** (`net8.0`, xUnit) — folders: `Services/`, `Hotkeys/`, `Logging/`, `Models/`; references `CLIHub.Core` only

## Documentation

**`docs/`** — project documentation (markdown):

- `vision.md` — vision, goals, audience, capability roadmap
- `architecture.md` — architecture, responsibilities, plugin format, technology decisions, conventions
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
