# Repository Structure

Use this file for navigation. Current behavior and architecture are summarized in
[`docs/project-context.md`](project-context.md) and [`docs/architecture.md`](architecture.md).

## Layout

```text
.
├── src/
│   ├── CLIHub.Core/          # UI-independent Core logic, services, models, and infrastructure
│   └── CLIHub/               # WPF application, tray, windows, startup, and UI workflows
├── tests/
│   ├── CLIHub.Core.Tests/    # Core-only xUnit tests
│   └── CLIHub.Tests/         # Application and WPF-dependent xUnit tests
├── docs/                     # Current project, architecture, release, glossary, and ADR docs
├── openspec/
│   ├── specs/                # Durable behavior contracts
│   └── changes/archive/      # Completed change history
├── .github/workflows/        # CI and tag-driven release workflows
├── assets/                   # Application icon source material
├── ui/mockups/               # UI design references, not build inputs
├── .opencode/               # OpenCode tooling configuration and skills
├── .pi/                     # Pi tooling configuration and prompts
├── CLIHub.sln
├── Directory.Build.props
├── global.json
├── .editorconfig
├── README.md
├── AGENTS.md
├── CHANGELOG.md
└── RELEASE-NOTES.md
```

Build output is redirected to the git-ignored `artifacts/` and `obj/` directories.

## Source Navigation

- `CLIHub.Core` is organized by ownership: `Configuration/`, `Projects/`, `Plugins/`, `Agents/`, `Updates/`,
  `Infrastructure/`, and `Composition/`, plus shared `Models/`, `Hotkeys/`, `Logging/`, `Formatting/`, and
  embedded `SeedPlugins/`.
- `CLIHub` contains `Views/`, `ViewModels/`, `Themes/`, `Hotkeys/`, `Converters/`, `Interop/`, and the
  application composition/startup files.
- Core contracts are colocated with their owning subsystem. Exact class locations should be checked in the
  source rather than inferred from this summary.
- ViewModel test builders are under `tests/CLIHub.Tests/Builders/`; test-specific usage is documented in
  [`tests/CLIHub.Tests/README.md`](../tests/CLIHub.Tests/README.md).

## Documentation Navigation

- [`docs/project-context.md`](project-context.md) — compact current context for agents.
- [`docs/architecture.md`](architecture.md) — current architecture index.
- [`docs/architecture/`](architecture/) — focused startup, configuration, plugin, process, and UI topics.
- [`docs/glossary.md`](glossary.md) — canonical terminology.
- [`docs/releasing.md`](releasing.md) — release runbook.
- [`docs/adr/`](adr/) — architectural rationale.
- [`openspec/specs/`](../openspec/specs/) — behavior contracts.
- [`openspec/changes/archive/`](../openspec/changes/archive/) — historical proposals, designs, and tasks.

## Root Configuration

- `Directory.Build.props` centralizes output paths, development version, and build style enforcement.
- `global.json` pins the SDK policy.
- `.editorconfig` defines repository-wide style rules.
- `openspec/config.yaml` contains the compact grounding context for OpenSpec artifacts.
