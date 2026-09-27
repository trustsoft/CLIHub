# Repository Structure

```
.
├── src/                # Source code
│   ├── CLIHub.Core/    # Business logic, services, models (no WPF dependencies)
│   ├── CLIHub/         # WPF application, UI, system tray integration
│   └── CLIHub.Tests/   # Unit and integration tests
├── docs/               # Project documentation
├── openspec/           # Spec-driven planning artifacts (OpenSpec CLI)
├── .opencode/          # OpenCode CLI configuration and skills
├── .idea/              # JetBrains Rider project settings
├── .git/               # Git version control metadata
├── artifacts/          # Build output (git-ignored)
│   ├── CLIHub/         # WPF application binaries
│   ├── CLIHub.Core/    # Core library binaries
│   └── CLIHub.Tests/   # Test binaries
├── obj/                # Intermediate build artifacts (git-ignored)
├── CLIHub.sln          # Visual Studio solution file
├── Directory.Build.props # MSBuild properties for all projects
├── .gitignore          # Git exclusions
└── AGENTS.md           # OpenCode agent instructions```

## Source Code

**`src/`** - Three-project solution architecture:

- **`CLIHub.Core/`** - Core business logic layer (class library, .NET 8)
  - `Models/` - Domain models: Plugin, Project, AppConfig, PluginCommand
  - `Services/` - Core services: ConfigService
  - `Interfaces/` - Service contracts: IConfigService
  - No dependencies on WPF or UI frameworks
  - Dependencies: Serilog, System.Text.Json
  
- **`CLIHub/`** - WPF application layer (.NET 8 Windows)
  - `Windows/` - WPF windows: MainWindow
  - `ViewModels/` - MVVM view models (to be created)
  - `Resources/` - Icons, images, styles
  - `App.xaml` - Application entry point
  - Dependencies: H.NotifyIcon.Wpf, Microsoft.Extensions.DependencyInjection, Serilog
  - Project reference to CLIHub.Core
  
- **`CLIHub.Tests/`** - Automated tests (xUnit, .NET 8)
  - `Services/` - Service tests
  - `Models/` - Model tests
  - Dependencies: xUnit, Moq, coverlet
  - Project reference to CLIHub.Core

See [architecture.md](architecture.md) for detailed design decisions, technology stack, and dependency flow.

## Documentation

**`docs/`** - Project documentation (markdown files):

- `vision.md` - Project vision, goals, audience, capability roadmap
- `architecture.md` - Solution architecture, technology decisions, conventions
- `repo-structure.md` - This file

## Planning Artifacts

**`openspec/`** - Spec-driven planning workflow managed via the OpenSpec CLI:

- `config.yaml` - Project configuration and compressed grounding context
- `changes/` - Active and archived change proposals (design docs, specs, tasks)

See `openspec/config.yaml` for project context and [OpenSpec documentation](https://github.com/Fission-AI/OpenSpec) for workflow details.

## Tooling Configuration

**`.opencode/`** - OpenCode CLI configuration, custom skills, and commands specific to this project.

**`.idea/`** - JetBrains Rider IDE project settings. User-specific files are not committed.

**`.git/`** - Git version control metadata.

## Solution Files

**`CLIHub.sln`** - Visual Studio solution file at repository root, referencing all projects under `src/`.

**`Directory.Build.props`** - MSBuild properties file at repository root that configures build output paths for all projects:
- Sets `BaseOutputPath` to `artifacts/` for compiled binaries
- Sets `BaseIntermediateOutputPath` to `obj/` for intermediate build files

## Build Output (git-ignored)

**Centralized build structure:**
- **`artifacts/`** - Final compiled binaries (DLLs, EXEs) organized by project and configuration
  - Example: `artifacts/CLIHub/Debug/net8.0-windows/CLIHub.dll`
  - Example: `artifacts/CLIHub.Core/Debug/net8.0/CLIHub.Core.dll`
- **`obj/`** - Intermediate build artifacts at repository root (MSBuild temporary files, generated code)
  - Example: `obj/CLIHub/Debug/net8.0-windows/`

Both directories are excluded from version control via `.gitignore`.
