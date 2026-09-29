# Proposal

## Why

The launch window is still the original scaffold: a light, header-plus-status-bar layout where agent commands live in one bottom button row and therefore apply only to the selected row. The approved mockup (`ui/mockups/popup-split.png`) defines a dark, task-oriented launcher where a project and an agent are chosen and acted on in one motion. Everything the redesign depends on is already delivered — resizable panes, availability dimming/filtering, version resolution, favorites in the project service, the Settings window, and the tray actions — so this is the natural next step and the last remaining item in the vision's launch-window direction.

## What Changes

- Build a new `LaunchWindow` as a copy of `MainWindow`, leave `MainWindow` untouched while the new window is developed, and then make `LaunchWindow` the full replacement for the application's window role: DI registration, tray show/toggle, the global-hotkey target, and the startup-visibility behavior all move to it. `MainWindow` stays in the repository unchanged, no longer used by the running application, as a reference for the window it replaced.
- Redesign the launch window to the dark two-pane layout in `ui/mockups/popup-split.png`.
- Add pane headers (`PROJECTS`, `AGENTS`), each with an `Actions ▾` menu.
- Project rows: rounded logo thumbnail, bold name, middle-ellipsized dimmed path, favorite marker (`★`), and a left accent bar on the selected row.
- Agent rows: logo, name, version beneath the name, and inline **▶ Launch** / **↻ Resume** buttons; agents unavailable in the project stay dimmed.
- Replace the status bar and the bottom command row with a footer: `CLIHub`, a version pill, the "Check for updates" button kept immediately to the right of that pill, and icon buttons for add project, Settings, and Exit. Transient status and error messages surface in the footer.
- Move the remaining agent commands (init, update, version, refresh) and the "only agents available in project" filter into the `AGENTS` Actions menu.
- Add "Toggle Favorite" to the `PROJECTS` Actions menu (the `IProjectService` favorite support already exists but no window exposes it).
- Apply a dark theme to the launch window.
- Move launch-window state and commands out of code-behind into a view model, per the project's MVVM convention.
- Move middle-ellipsis path formatting into `CLIHub.Core` so it is unit-testable.

## Capabilities

### New Capabilities

- `launch-window-theme`: the dark visual theme (palette and control styling) applied to the launch window.

### Modified Capabilities

- `main-window-layout`: pane headers with per-pane Actions menus, footer actions, version pill and update-check control, row presentation (logo thumbnail, middle-ellipsized path, favorite marker, selection accent), inline launch/resume per agent row, and scrolling long lists.
- `project-management`: marking and unmarking a favorite from the launch window.

## Impact

- **UI**: new `src/CLIHub/Windows/LaunchWindow.xaml(.cs)`; new view models under `src/CLIHub/ViewModels/`; new converters under `src/CLIHub/Converters/` (middle ellipsis); new theme resources merged from `src/CLIHub/App.xaml`; `ServiceRegistration.cs`, `TrayIconController.cs`, `GlobalHotkeyService.cs`, and `App.xaml.cs` rewired to `LaunchWindow`; `AgentItem.cs` moved to `src/CLIHub/ViewModels/` (`MainWindow.xaml.cs` receives a single `using` directive so it still compiles); `src/CLIHub/Windows/MainWindow.xaml(.cs)` kept as-is and no longer wired.
- **Core**: a platform-independent middle-ellipsis path formatter in `src/CLIHub.Core`, with unit tests in `tests/CLIHub.Tests`.
- **No changes** to configuration schema, plugin descriptors, dependencies, or the tray menu's structure.
- **Docs**: `docs/architecture.md` (window inventory), `docs/repo-structure.md` (new files and folders), `ui/mockups/README.md`, and `docs/vision.md`.
- **Non-goals**: restyling the Settings window, installing updates from the tray, any tray-menu redesign, and removing the retained `MainWindow`.
