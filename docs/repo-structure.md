# Repository Structure

```
.
├── src/                # Source code (not yet created)
│   ├── CLIHub.Core/    # Business logic, services, models (no WPF dependencies)
│   ├── CLIHub/         # WPF application, UI, system tray integration
│   └── CLIHub.Tests/   # Unit and integration tests
├── docs/               # Project documentation
├── assets/             # Application icon sources (PNG, design files)
├── openspec/           # Spec-driven planning artifacts (OpenSpec CLI)
├── .opencode/          # OpenCode CLI configuration and skills
├── .idea/              # JetBrains Rider project settings
├── .git/               # Git version control metadata
├── bin/                # Build output (git-ignored)
├── obj/                # Intermediate build artifacts (git-ignored)
├── CLIHub.sln          # Solution file (to be created)
└── [temp files]        # Temporary scaffold files (to be relocated)
```

## Source Code

**`src/`** (planned structure, not yet created)

Three-project solution architecture:

- **`CLIHub.Core/`** - Core business logic layer
  - Models, services, configuration management
  - Plugin discovery and validation
  - Process spawning and Windows Terminal integration
  - No dependencies on WPF or UI frameworks
  
- **`CLIHub/`** - WPF application layer
  - System tray integration (H.NotifyIcon.Wpf)
  - UI windows and dialogs
  - Dependency injection setup
  - Application entry point and lifetime management
  
- **`CLIHub.Tests/`** - Automated tests
  - Unit tests for Core services
  - Integration tests for configuration and plugin loading

See [architecture.md](architecture.md) for detailed design decisions, technology stack, and dependency flow.

## Documentation

**`docs/`** - Project documentation (markdown files):

- `vision.md` - Project vision, goals, audience, capability roadmap
- `architecture.md` - Solution architecture, technology decisions, conventions
- `repo-structure.md` - This file

## Assets

**`assets/`** - Application icon source files in various formats and sizes, including PNG images used to generate the final `.ico` for the WPF application and system tray.

## Planning Artifacts

**`openspec/`** - Spec-driven planning workflow managed via the OpenSpec CLI:

- `config.yaml` - Project configuration and compressed grounding context
- `changes/` - Active and archived change proposals (design docs, specs, tasks)

See `openspec/config.yaml` for project context and [OpenSpec documentation](https://github.com/Fission-AI/OpenSpec) for workflow details.

## Tooling Configuration

**`.opencode/`** - OpenCode CLI configuration, custom skills, and commands specific to this project.

**`.idea/`** - JetBrains Rider IDE project settings. User-specific files are not committed.

**`.git/`** - Git version control metadata.

## Solution Files (to be created)

**`CLIHub.sln`** - Visual Studio solution file at repository root, referencing all projects under `src/`.

Optional future additions:
- `Directory.build.props` - Common MSBuild properties for all projects
- `src/Directory.build.props` - Source-specific build configuration

## Temporary Root Files (to be relocated)

The following files currently exist in the repository root from an early scaffold attempt and will be **moved into `src/CLIHub/`** once the solution structure is finalized:

- `App.xaml`, `App.xaml.cs` - WPF application entry point
- `AssemblyInfo.cs` - Assembly metadata
- `CLIHub.csproj` - Project file (will move under `src/CLIHub/`)
- `MainWindow.xaml`, `MainWindow.xaml.cs` - Default WPF window template
- `Models/`, `Services/`, `Windows/`, `Resources/` - Scaffolded code directories

**Do not treat these as the final structure** - they predate the `src/` reorganization decision documented in `architecture.md`.

## Build Output (git-ignored)

- **`bin/`** - Compiled binaries (per-project output for each target framework)
- **`obj/`** - Intermediate build artifacts (MSBuild temporary files)
