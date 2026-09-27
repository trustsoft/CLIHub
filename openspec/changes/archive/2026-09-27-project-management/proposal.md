## Why

The MVP launched AI agents but had no notion of *which project* to work in — every launch used the process working directory. Real usage needs a tracked set of project folders with context, so agents can be launched in the right directory and the tray shows what you are working on.

## What Changes

- Introduce a project management service that tracks a user's projects (folder path, name, favorite flag, last-used time)
- Persist projects in the existing `%APPDATA%\CLIHub\config.json` via `ConfigService`
- Track a "current project" that provides launch context
- Auto-detect a project logo from well-known image filenames in the project folder, with a default fallback
- Maintain a recent-projects list ordered by last use
- Support marking/unmarking favorites
- Surface current project and recent projects in the system tray context menu
- Launch agents into the selected project directory

## Capabilities

### New Capabilities

- `project-management`: Registering, listing, selecting, and persisting project directories, including current-project tracking, recent ordering, favorites, and logo detection

### Modified Capabilities

<!-- None: no main specs exist yet (application-scaffold was archived without syncing). -->

## Impact

**New code:**
- `src/CLIHub.Core/Interfaces/IProjectService.cs` — project service contract
- `src/CLIHub.Core/Services/ProjectService.cs` — implementation backed by `ConfigService`
- `src/CLIHub.Core/Services/ProjectLogoResolver.cs` (or method on `ProjectService`) — logo detection

**Modified code:**
- `src/CLIHub/App.xaml.cs` — tray menu shows current project and recent projects
- `src/CLIHub/Windows/MainWindow.xaml(.cs)` — project list and selection
- `src/CLIHub/Windows/MainWindow.xaml.cs` — launch into selected project directory

**Data:**
- `%APPDATA%\CLIHub\config.json` — already has `AppConfig.Projects` and `AppConfig.CurrentProjectId`; now populated

**Reuses (no changes):**
- `CLIHub.Core.Models.Project` (id, name, path, isFavorite, lastUsed, logoPath)
- `CLIHub.Core.Services.ConfigService` (atomic JSON persistence)

**No breaking changes.**
