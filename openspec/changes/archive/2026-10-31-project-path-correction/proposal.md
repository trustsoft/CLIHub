# Project Path Correction: relocate a manual project folder

## Why

A manual project folder may be moved or renamed after registration. The project row keeps its identity (ID, provenance, favorite flag, last-used timestamp, current-selection status) but points to a path that no longer exists. Verified at HEAD `ccf4285`:

- `IProjectService` (`src/CLIHub.Core/Projects/IProjectService.cs:8-68`) offers `AddProject`, `RemoveProject`, `ToggleFavorite`, and `SetCurrentProject` but no operation to re-point a project to a new folder location.
- `ProjectService` (`src/CLIHub.Core/Projects/ProjectService.cs`) derives a project's name from its folder name (`CreateProject` `:168`) and persists both name and path in configuration. `RemoveProject` invalidates the logo cache entry (`:193`) but there is no path-update operation.
- The Settings Projects page (`src/CLIHub/Views/SettingsProjectRegistryPage.xaml`) displays a "Folder not found" chip (`:157-161`) when `!IsFolderAvailable`, but provides no way to correct the path — the user must remove the project and re-register it, losing its identity, provenance, and favorite flag.

This change adds a "Change folder…" action to manual project rows on the Settings Projects page. The user selects a new folder; the project keeps its identity, gets the corrected path, re-derives its display name from the new folder name, invalidates its logo cache entry, and re-enriches immediately. Project Scanning still governs discovered projects, so they remain read-only on that page.

## What Changes

- **API:** `IProjectService` adds `void ChangeProjectPath(string projectId, string newFolderPath)`. The service validates the new path (non-blank, fully qualified, existing directory, normalized), confirms it is not already registered to a **different** project (same project is a no-op), updates both the normalized path and the folder-derived name, invalidates the logo cache entry for that project ID, persists immediately (atomic config write), and raises `ProjectsChanged`.
- **UI:** The Settings Projects page adds a "Change folder…" action (icon: `FolderOpen` with a distinct visual treatment, placed between the project metadata and the existing actions) visible only on manual project rows. The command opens the folder picker, passes the selected path to the service API, handles validation errors, and re-loads the enriched models.
- **Behavior:** The change preserves the project's ID, provenance (`Manual`), favorite flag, last-used timestamp, and current-selection status. Only the path and derived name change; the project folder's contents are never touched.
- **Durability:** The spec delta modifies the existing "Settings project registry page" requirement to add scenarios for the valid change, invalid/missing/duplicate path, discovered-row exclusion, and immediate persistence.

## Impact

- Affected specs: `project-management` (MODIFIED: Settings project registry page; ADDED scenarios for manual project path correction).
- Affected code:
  - `src/CLIHub.Core/Projects/IProjectService.cs` + `ProjectService.cs` — add `ChangeProjectPath` with validation, path normalization, deduplication, logo-cache invalidation, and persistence
  - `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs` — add `ChangePathCommand` and `ChangePath(ProjectModel)` method
  - `src/CLIHub/Views/SettingsProjectRegistryPage.xaml` — add "Change folder…" button (manual rows only)
  - `tests/CLIHub.Core.Tests/Projects/ProjectServiceTests.cs` — cover validation (blank, non-existent, invalid path), deduplication (same project = no-op, different project = error), path and name updates, logo invalidation, and persistence round-trip
  - `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs` — cover command availability (manual only), successful change, validation errors, dialog cancellation, and immediate enrichment
- Non-goals:
  - no editable display name or manual rename feature (the name is always derived from the folder name)
  - no path correction for discovered projects (Project Scanning governs them)
  - no file-system moves or copies; the feature re-points the project record only
  - no config schema change or migration (the existing `Project.Path` and `Project.Name` fields hold the corrected values)
  - no launch-window UI changes; path correction is a Settings-page-only action

## Dependencies

None. The change extends the existing project mutation API and enrichment flow without altering their contracts.
