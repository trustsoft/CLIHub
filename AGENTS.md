# CLIHub Agent Instructions

## Project Status

**Active WPF application.** Delivered capabilities (see `openspec/specs/`): agent commands, detection, version display, availability display/filtering, app lifecycle, hotkey support, logging, plugin seeding, project management, update checking (check, download, apply), preferences window, window layout, launch window theme, settings theme, release notes, release notes display.

## Documentation

Single source of truth for each topic — link, don't duplicate:

- `README.md` — user-facing overview and quickstart
- `docs/vision.md` — vision, goals, roadmap
- `docs/glossary.md` — canonical terminology (project, agent, plugin, agent command, availability); use these terms in docs, specs, and code
- `docs/architecture.md` — architecture, responsibilities, plugin format, tech stack, `%APPDATA%` layout, security
- `docs/repo-structure.md` — repository layout (where everything lives)
- `docs/changelog-and-release-notes.md` — changelog & release-notes plan (draft, decision pending)
- `openspec/specs/` — durable capability specs; `openspec/changes/archive/` — completed changes

## Project Structure

Application source lives under `src/` and tests under `tests/`; the solution is `CLIHub.sln` at the repository root. Do not create projects in the repo root. Full layout: [docs/repo-structure.md](docs/repo-structure.md).

| Project | Path | Purpose |
|---------|------|---------|
| `CLIHub.Core` | `src/CLIHub.Core` | Platform-independent core logic: models, services, interfaces, plugin/agent handling, config, logging, hotkey parsing. No WPF. |
| `CLIHub` | `src/CLIHub` | WPF application: startup/DI, system tray, global hotkey, windows, converters. |
| `CLIHub.Tests` | `tests/CLIHub.Tests` | xUnit tests for `CLIHub.Core`; references Core only. |

## Technology Stack

.NET 8 (WPF for UI) · H.NotifyIcon.Wpf (tray) · Serilog (logging) · Microsoft.Extensions.DependencyInjection (DI) · Velopack (updates). Details: [docs/architecture.md](docs/architecture.md#technology-stack).

## Key Conventions

- **All code, comments, and UI text in English** - even though Russian is allowed for docs
- **Nullable reference types enabled** - use `?` for nullable types
- **JSON config** at `%APPDATA%\CLIHub\config.json` with camelCase serialization
- **Plugin system** uses JSON descriptors in `%APPDATA%\CLIHub\plugins\<plugin-id>/plugin.json`
- **Log location:** `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log` with 7-day retention

## Code Style

- **Nullable reference types** enabled globally.
- **Implicit usings** enabled — no need for `using System;` etc.
- **File-scoped namespaces** (`namespace X;` not block-scoped).
- **Using placement**: the namespace declaration must be followed by using directives (place `using` directives after the file-scoped `namespace X;`).
- **Braces**: Use curly braces for if statements and loops.
- **Naming**: PascalCase for public members, `_camelCase` for private fields. Test methods use `MethodOrScenario_Condition_ExpectedResult` pattern.
- **XML documentation**: public types and members carry XML doc comments. Canonical examples: `src/CLIHub.Core/Models/AgentCommandKind.cs` and `src/CLIHub.Core/Models/AgentCommandResult.cs`. Conventions:
  - `<summary>` is always multi-line; content lines are indented one space past the tag alignment (`///` followed by three spaces):
    ```csharp
    /// <summary>
    ///   Outcome of executing an agent command.
    /// </summary>
    ```
  - Other tags (`<param>`, `<returns>`, `<exception>`, …) stay on one line, padded with a single space inside both ends:
    ```csharp
    /// <param name="Success"> Whether the command succeeded. </param>
    ```
  - Doc text is full English sentences: capital first letter, trailing period.
  - Positional record parameters are documented with `<param name="...">` tags in declaration order.
- **Async patterns**: `async Task` / `ValueTask` used extensively; avoid `async void`.
- **Reactive extensions (if needed)**: R3 (`ObservableCollections.R3`, `R3`) used for reactive patterns in ViewModels.
- **MVVM**: Use the Model-View-ViewModel pattern for the WPF UI — keep views (XAML) and code-behind thin; put state, commands, and logic in view models under `src/CLIHub/ViewModels/`. (Some existing windows still carry logic in code-behind and are being migrated as they change.)

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

User data lives under `%APPDATA%\CLIHub\` (config, logs, plugins, cache). Full layout: [docs/architecture.md](docs/architecture.md#file-system-layout). Repository layout: [docs/repo-structure.md](docs/repo-structure.md).

## What to Avoid

- Do NOT use Windows Forms components (`System.Windows.Forms.*`)
- Do NOT create WPF projects without a `.sln` file
- Do NOT put source code in repo root instead of `src/`
- Do NOT implement before discussing structure with user
