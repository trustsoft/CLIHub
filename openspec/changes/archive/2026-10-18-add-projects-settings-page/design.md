## Context

See `proposal.md` for motivation. Relevant current state:

- `IProjectService` is the singleton owner of the project registry. `GetAllProjects` returns `Project` models carrying `Source` (`Manual`/`Discovered`), `Path`, `Name`, and `LastUsed`; `AddProject` registers with manual provenance; `RemoveProject` clears the current selection when needed and persists atomically.
- Project mutations raise `ProjectsChanged`. The Launch Window (`ProjectPaneController`), tray projection, and status message already subscribe and refresh from it.
- The Settings window is a sidebar of transient page view models. `SettingsViewModel.AllPages` declares page keys, titles, descriptions, groups, icons, and implementation flags; `SettingsWindow.xaml` stacks one `UserControl` per page and toggles visibility on `SelectedPage.Key`.
- The "CLI Agents" page already follows a management-page pattern: a separate transient section view model (`IAgentManagementSection` → `AgentManagementViewModel`) injected into `SettingsViewModel`, refreshed from `SettingsViewModel.Load()` and disposed by `SettingsViewModel.Dispose()`.
- The existing class `SettingsProjectsPage` is the Project Discovery page (scanner configuration), not a registry page.
- `IProjectDialogService.ConfirmProjectRemoval` already exists and is used by the Launch Window; `IProjectDialogService.SelectProjectFolder`/`SelectFolder` is already used by Settings for watched-root selection.
- `SettingsNavigationTests` pins the page count, order, and keys.

## Goals / Non-Goals

**Goals:**

- Add the Projects page as a self-contained section that reuses the existing registry service and dialog service.
- Keep the page live while open, including changes made by other surfaces or by an in-flight scan.
- Keep the Launch Window workflow unchanged; adding from Settings must not change the current project.
- Make the discovered/manual distinction explicit and prevent actions that the scanner would undo.

**Non-Goals:**

- No `CLIHub.Core` domain or configuration changes, no new persisted preferences, no config schema migration.
- No editing of project names or paths, no bulk cleanup, no removal of discovered projects, no logo management on this page.
- No replacement of the Launch Window as the primary add/select/remove surface; the page is an administrative view.

## Decisions

### Separate section view model behind an interface

Add `IProjectRegistrySection` (row collection, add/remove commands, error text, `Load()`, `IDisposable`) and a `ProjectRegistryViewModel` implementation, registered transient in `ServiceRegistration` and injected into `SettingsViewModel` alongside `IAgentManagementSection`.

- **Why:** `SettingsViewModel` is already large and the agents page established this boundary; the section stays unit-testable without constructing the whole Settings window.
- **Alternatives rejected:** adding the commands and collection directly to `SettingsViewModel` (further growth, harder tests); reusing `ProjectPaneController` (it is launch-window-specific: it sets the current project on add, works on the selected project rather than a list row, and uses `PromptState` to protect the launch window's hide-on-deactivate behavior, which Settings does not have).

### Row view model with computed display state

`ProjectRegistryItem` wraps a `Project` and exposes `Id`, `Name`, `Path`, `IsManual`, a single-letter provenance mark (`M`/`D`) with the full label as its tooltip and accessible name, and `IsFolderMissing` computed with `Directory.Exists` during refresh.

- **Why:** `Project` is a Core model; provenance label and folder existence are presentation concerns. Computed flags keep the XAML free of converters and make the states directly testable.
- **Alternatives rejected:** binding `Project` directly with converters (less testable, leaks display logic into XAML); adding folder-existence to Core (not needed by any domain rule).

### Add and remove semantics

- **Add:** a path field with a "Browse…" folder picker and a "Register" action, mirroring the watched-directory editor; the path is validated (non-blank, fully qualified, existing), duplicates are rejected with an error, then `IProjectService.AddProject(path)` is called and the field is cleared. Never call `SetCurrentProject`.
- **Remove:** `IProjectDialogService.ConfirmProjectRemoval(name)`, then `IProjectService.RemoveProject(id)`, then refresh.
- **Open folder:** `IExternalLauncher.Open(path)` for every project row; the action is disabled when the folder is missing. Opening is not a registry mutation, so it is available for discovered projects too.
- **Errors:** registration, removal, and open failures are shown in a section-level error line directly under the add row, mirroring the watched-directory error pattern.

`RemoveProject` already clears `currentProjectId` when the removed project is current, so no extra bookkeeping is needed.

### Discovered projects are read-only

Rows for discovered projects expose no remove command, and the page explains that Project Discovery manages them.

- **Why:** `ProjectScanningService.ReconcileDiscoveredProjects` re-registers any discovered folder that still exists under a watched root, so a manual removal would visibly come back on the next scan. Deferring that ownership to the scanner avoids a misleading action.
- **Alternative rejected:** allowing removal and adding an exclusion list — new durable behavior and a new persisted setting, out of scope for this change.

### Live refresh through `ProjectsChanged`

The section subscribes to `IProjectService.ProjectsChanged` and rebuilds the rows; `Load()` also rebuilds on each window open. `Dispose()` unsubscribes.

- **Why:** a scan can add or remove discovered projects while the Settings window is open (notably from the Project Discovery page), and the tray can remove a project concurrently.
- **Risk control:** `SettingsViewModel` is transient and the project service is a singleton, so the unsubscribe in `Dispose()` is mandatory; a test asserts the subscription is removed.

### Page registration and naming

- New page entry: key `projects-registry`, title `Projects`, group `Engines & Repos`, implemented, inserted before the existing `projects` entry so the registry is listed before scanner configuration.
- New view: `SettingsProjectRegistryPage`; the existing `SettingsProjectsPage` (Project Discovery) keeps its name.
- `SettingsViewModel` constructor gains the new section parameter; `ServiceRegistration` registers the interface and passes it.
- Content uses the existing section header, list-row card, and dashed add-row patterns from the Project Discovery page, and the page states that project changes apply immediately.

Rejected: renaming `SettingsProjectsPage` to `SettingsDiscoveryPage` — accurate but touches XAML, code-behind, and tests for no behavioral gain; can be a separate cleanup.

### Tests

- New `ProjectRegistryViewModelTests`: rows for manual and discovered projects with provenance and missing-folder flags; add registers a manual project and leaves the current project unchanged; duplicate add creates no duplicate; remove asks for confirmation and removes on confirm, keeps on cancel; discovered rows expose no remove; changes refresh the rows; dispose unsubscribes from `ProjectsChanged`.
- Update `SettingsNavigationTests` for nine pages and the new key order.
- Follow the `MethodOrScenario_Condition_ExpectedResult` naming and the existing Moq-based fixtures.

## Risks / Trade-offs

- **Dialogs are owned by `Application.Current.MainWindow`, not the Settings window** → the existing watched-root browse already uses the same service from Settings, so the pattern is proven; verify the removal confirmation appears above Settings and, if not, extend `IProjectDialogService` with an owner parameter as a follow-up.
- **Event subscription leak if `Dispose` is missed** → `SettingsViewModel` is transient and recreated per window; unsubscribe in `Dispose()` and cover it with a test.
- **Removing the current project clears the selection** → existing service behavior; the Launch Window and tray refresh through `ProjectsChanged`, and a test covers the page reflecting the removal.
- **Synchronous `Directory.Exists` per row** → acceptable for a typical registry size; if large network paths make refresh slow, move the check to a background pass later without changing behavior.
- **Discovered rows being read-only may surprise users** → the hint text points to Project Discovery and explains why.

## Migration Plan

No data or configuration migration. The page reads and mutates the existing registry through the existing service. Rolling back removes the page entry, view, section view model, and DI registration; stored project data is untouched.

## Open Questions

- Row ordering and grouping (manual first vs pure alphabetical) and whether to show project logos are visual details that can be settled during implementation without affecting the spec or the task breakdown.
