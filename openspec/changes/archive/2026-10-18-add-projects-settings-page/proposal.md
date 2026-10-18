## Why

Manually registered projects can only be managed from the Launch Window, where the user must first select a project to remove it, and where the full registry (paths, provenance, missing folders) is not visible. The Settings window already configures project discovery and reports the manual/discovered split, but offers no way to audit or maintain the manually registered projects. Users need one place to view the complete project registry and clean up manual entries without switching to the launch workflow.

## What Changes

- Add a **Projects** page to the Settings window under the "Engines & Repos" group, next to "Project Discovery".
- The page lists every registered project with its name, full path, provenance (manual or discovered), and an indicator when the folder no longer exists.
- Manual projects can be **added** from the page through the folder picker and **removed** after confirmation. Adding from Settings SHALL NOT change the current project selection, unlike adding from the Launch Window.
- Discovered projects are shown **read-only**, with a hint that Project Discovery owns them, because a discovered project removed by hand would be registered again by the next scan.
- Project changes on the page apply immediately, independently of the footer Save and Cancel, and reach the Launch Window and tray through the existing project-change notification.
- "Project Discovery" keeps its name and scope (scanner configuration); the new page manages the project registry.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `project-management`: add a requirement for the Settings project registry page — a full registry view with provenance and folder-existence status, manual registration that does not change the current project, manual removal with confirmation, and read-only discovered projects.
- `preferences-ui`: extend the Settings sections requirement from eight to nine pages and describe the new "Projects" page alongside "Project Discovery".

## Impact

- Affected code: `src/CLIHub/Views` (new settings page and its entry in the content switch, sidebar page list), `src/CLIHub/ViewModels/Settings` (new registry section view model and page registration), `src/CLIHub/Composition/ServiceRegistration.cs` (dependency wiring), and the related tests under `tests/CLIHub.Tests`.
- Reuses `IProjectService` (`GetAllProjects`, `AddProject`, `RemoveProject`) and `IProjectDialogService`; no `CLIHub.Core` domain or persistence changes and no configuration schema change.
- The Launch Window and tray behavior are unchanged; the existing project-change notification keeps every surface in sync.
