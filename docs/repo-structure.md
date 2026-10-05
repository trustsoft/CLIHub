# Repository Structure

```
.
├── src/                     # Application source (two projects)
│   ├── CLIHub.Core/         # Business logic, services, models (no WPF dependencies)
│   └── CLIHub/              # WPF application, UI, system tray, hotkey
├── tests/                   # Test projects
│   └── CLIHub.Tests/        # Unit tests (xUnit)
├── docs/                    # Project documentation
├── .github/workflows/       # GitHub Actions (ci.yml: build+test; release.yml: tag -> GitHub Releases)
├── assets/                  # Application icon source files (not in the build)
├── ui/                      # UI mockups and design references (not in the build)
├── openspec/                # Specs (openspec/specs) + archived changes
├── .opencode/               # OpenCode CLI configuration and skills
├── .idea/                   # JetBrains Rider project settings
├── .vs/                     # Visual Studio settings (git-ignored)
├── .git/                    # Git metadata
├── artifacts/               # Build output (git-ignored)
├── obj/                     # Intermediate build artifacts (git-ignored)
├── CHANGELOG.md             # Technical changelog (developers)
├── RELEASE-NOTES.md         # User-facing release notes (embedded into the app)
├── CLIHub.sln               # Visual Studio solution file
├── Directory.Build.props    # MSBuild properties (build output paths)
├── .editorconfig            # Code style rules (root; enforced at build)
├── .gitignore               # Git exclusions
└── AGENTS.md                # OpenCode agent instructions
```

## Source Code

**`src/`** — application projects; **`tests/`** — test projects. Responsibilities, dependency rules, and the plugin descriptor format are in [architecture.md](architecture.md); the folder inventory is below.

- **`src/CLIHub.Core/`** (`net8.0`, no WPF) — subsystem folders and matching namespaces: `Configuration/` (the shared `ConfigService` persistence boundary plus the narrower `PreferencesStore` and `ProjectStateStore` adapters), `Projects/`, `Plugins/`, `Agents/`, `Updates/`, `Infrastructure/` (`FileSystem/`, `Processes/`, `Persistence/`, `Windows/`), and `Composition/`; shared folders remain `Models/`, `Hotkeys/`, `Logging/`, `Formatting/` (display formatting helpers such as `MiddleEllipsisFormatter`), and `SeedPlugins/` (embedded built-in agent descriptors + logos). Core contracts are colocated with their owning subsystem; the former `CLIHub.Core.Services` and `CLIHub.Core.Interfaces` namespaces are removed. Core is UI-independent and Windows-aware; WPF is not referenced by this project.
- **`src/CLIHub/`** (`net8.0-windows`) — folders: `Views/`, `Hotkeys/`, `Converters/`, `Interop/`, `ViewModels/`, `Themes/` (`LaunchTheme.xaml` palette, `Sizing.xaml` metrics, `Controls.xaml` shared keyed styles including the `DarkToolTip` base, `DarkScrollBar.xaml` shared slim dark scrollbar, `SettingsStyles.xaml` settings-window styles, `LaunchWindowStyles.xaml` and `WhatsNewStyles.xaml` window-scoped styles, `IconGlyphs.cs` Segoe MDL2 glyph constants); the implicit dark tooltip style and shared converters live in `App.xaml`; files: `Program.cs` (entry point + Velopack bootstrap), `App.xaml(.cs)`, `ServiceRegistration.cs`, `IPreferenceApplier.cs`, `PreferenceApplier.cs`, `ISettingsLauncher.cs`, `SettingsLauncher.cs`, `IReleaseNotesLauncher.cs`, `ReleaseNotesLauncher.cs`, `TrayIconController.cs`, `AssemblyInfo.cs`, `app.ico`, `default-project.png`. `Views/LaunchWindow.xaml(.cs)` with `ViewModels/LaunchWindowViewModel.cs` and `ViewModels/MenuAction.cs` (the data-driven pane Actions menus) is the application window; `Views/SettingsWindow.xaml(.cs)` with `ViewModels/SettingsViewModel.cs` is the settings window; `Views/WhatsNewWindow.xaml(.cs)` shows the release notes; `Views/MainWindow.xaml(.cs)` is the pre-redesign window, retained for reference and no longer wired
- **`tests/CLIHub.Tests/`** (`net8.0`, xUnit) — folders: `Services/`, `Hotkeys/`, `Logging/`, `Formatting/`, `ReleaseNotes/`, `Models/`; references `CLIHub.Core` only

## Documentation

**`docs/`** — project documentation (markdown):

- `vision.md` — vision, goals, audience, capability roadmap
- `glossary.md` — canonical terminology used across docs, specs, and code
- `architecture.md` — architecture, responsibilities, plugin format, technology decisions, conventions
- `releasing.md` — step-by-step release runbook (notes, tag, verification, rollback)
- `repo-structure.md` — this file
- `changelog-and-release-notes.md` — the rationale and decision log behind the changelog & release notes (implemented by the `release-notes` change)

The two release-note documents live at the repository root rather than under `docs/` so they are the first
thing a contributor sees and so the build can embed the user-facing one by path:

- `CHANGELOG.md` — technical record of changes, for developers (Added / Changed / Fixed / Removed, spec
  references, `**BREAKING**` markers)
- `RELEASE-NOTES.md` — short user-facing notes (New / Improved / Fixed), written as plain text and embedded
  into `CLIHub.Core`, so the application can show them in the **What's New** window without network access

Both use the same version headings (`## <version> — <date>`) so a release lines up across the two files; the
format and the display rules are described in [architecture.md](architecture.md).

## Assets

**`assets/`** — application icon source files (design-tool output): `appIcon/DS4.1F/` and `appIcon/GLM5.3F/` with PNG sizes and `.ico` files. Used as source material; the final `app.ico` is copied into `src/CLIHub/`, and agent logos are embedded under `src/CLIHub.Core/SeedPlugins/`. Not included in the build.

## UI References

**`ui/`** — reference mockups for UI work (not part of the build):

- `ui/mockups/` — `popup-split.png` (launch window, delivered) and `settings.png` (Settings window), with a `README.md` describing each; see that README before further UI work

## Planning Artifacts

**`openspec/`** — spec-driven workflow via the OpenSpec CLI:

- `config.yaml` — project configuration and compressed grounding context
- `specs/` — durable capability specs (the source of truth for behavior)
- `changes/archive/` — completed changes (proposals, design, deltas, tasks)

See `openspec/config.yaml` for project context and [OpenSpec documentation](https://github.com/Fission-AI/OpenSpec) for workflow details.

## Tooling Configuration

**`.editorconfig`** — repository-wide code style rules (whitespace, naming, language style), one file at the root covering `src/`, `tests/`, and non-code files; enforced at build (see the `code-style` spec).
**`.opencode/`** — OpenCode CLI configuration, skills, and commands.
**`.idea/`**, **`.vs/`** — IDE settings (user-specific, not committed).
**`.git/`** — Git metadata.

## Solution Files

**`CLIHub.sln`** — solution at the repository root, referencing the two application projects under `src/` and the test project under `tests/`.

**`Directory.Build.props`** — MSBuild properties shared by all projects:
- `BaseOutputPath` → `artifacts/` (compiled binaries)
- `BaseIntermediateOutputPath` → `obj/` (intermediate files)
- `Version` → `0.7.0` (product version; the development default — a release takes its version from the pushed `v*` tag)
- `EnforceCodeStyleInBuild` → `true` (code style checked as part of the build)

## Build Output (git-ignored)

- **`artifacts/`** — final binaries per project/configuration, e.g. `artifacts/CLIHub/Debug/net8.0-windows/CLIHub.exe`
- **`obj/`** — intermediate build files, e.g. `obj/CLIHub/Debug/net8.0-windows/`
