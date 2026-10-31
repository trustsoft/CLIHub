# Design: project-path-correction

## Current state (verified in code)

| Concern | Today | File |
|---|---|---|
| Project mutation API | `IProjectService` (`src/CLIHub.Core/Projects/IProjectService.cs:8-68`) offers `AddProject` (with validation `:39-41`), `RemoveProject` (invalidates logo cache `:56-57`), `SetCurrentProject`, `ToggleFavorite`, and `ReconcileDiscoveredProjects` but no path-update operation | `src/CLIHub.Core/Projects/IProjectService.cs` |
| Project creation | `ProjectService.CreateProject` (`:163-181`) derives the project name from the folder name using `Path.GetFileName(normalized.TrimEnd(...))` (`:168`), falls back to the full path if the result is empty (`:175-178`), and assigns `Source`, `IsFavorite`, and `LastUsed` | `src/CLIHub.Core/Projects/ProjectService.cs` |
| Path normalization | `AddProject` (`:71-96`) validates non-blank (`:73-76`), normalizes via `ProjectPathPolicy.Normalize` (`:78`), requires an existing directory (`:80-83`), and deduplicates via `ProjectPathPolicy.AreEqual` (`:85-89`) before creating; duplicate returns the existing project (`:88`) | `src/CLIHub.Core/Projects/ProjectService.cs` |
| Logo cache invalidation | `RemoveProject` (`:184-202`) calls `this.logoResolver.Remove(id)` (`:193`) after removing the project from state; logo cache entries are keyed by project ID and persisted | `src/CLIHub.Core/Projects/ProjectService.cs` |
| Persistence | `Persist()` (`:255-259`) calls `projectStateStore.Update(state => this.projectState.CopyTo(state))` and raises `ProjectsChanged` event atomically; every mutation uses it | `src/CLIHub.Core/Projects/ProjectService.cs` |
| Path policy | `ProjectPathPolicy` (`src/CLIHub.Core/Projects/ProjectPathPolicy.cs`) offers `Normalize(string)` (`:13-15`: `Path.GetFullPath` + trim trailing separators) and `AreEqual(left, right)` (`:23-24`: normalized, case-insensitive) | `src/CLIHub.Core/Projects/ProjectPathPolicy.cs` |
| UI command infrastructure | `ProjectRegistryViewModel` (`src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs`) owns `BrowseCommand`, `RegisterCommand`, `RemoveCommand`, and `OpenFolderCommand` (`:47-52`); commands take `ProjectModel` as `CommandParameter` where appropriate; `IProjectDialogService.SelectFolder(title)` (`:14-19`) is available | `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs`, `src/CLIHub/Launching/IProjectDialogService.cs` |
| Row actions in XAML | `SettingsProjectRegistryPage.xaml` (`:171-201`) renders action buttons in a `SpacedPanel`: `OpenFolderCommand` (`:177-187`) and `RemoveCommand` (`:189-200`, `Visibility` bound to `CanRemove`) | `src/CLIHub/Views/SettingsProjectRegistryPage.xaml` |
| Enrichment | `ProjectModel` (`:153-160` in `src/CLIHub/ViewModels/Projects/ProjectModel.cs`) applies enriched snapshots in place; `ProjectEnricher` (referenced at `ProjectRegistryViewModel.cs:132`) re-enriches on `Load()` and project mutations trigger `ProjectsChanged` → `Load()` (`:259`) | `src/CLIHub/ViewModels/Projects/ProjectModel.cs`, `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs` |
| Discovery vs manual | `ProjectSource` enum governs provenance; `ReconcileDiscoveredProjects` (`:99-158` in `ProjectService.cs`) only touches `ProjectSource.Discovered` entries (`:132`), never manual ones; `ProjectModel.CanRemove` (`:112`) is `true` ⇔ `Source == ProjectSource.Manual` | `src/CLIHub.Core/Projects/ProjectService.cs`, `src/CLIHub/ViewModels/Projects/ProjectModel.cs` |

## Decision 1: API shape — `ChangeProjectPath(string projectId, string newFolderPath)`

The service API is:

```csharp
/// <summary>
///   Changes the folder path for a manually registered project. The project keeps its
///   identity, provenance, favorite flag, and last-used timestamp, but gets the new path
///   and a name re-derived from the new folder name. The project's logo cache entry is
///   invalidated.
/// </summary>
/// <param name="projectId"> The project identifier. </param>
/// <param name="newFolderPath"> The new folder path. </param>
/// <exception cref="ArgumentException">
///   The project ID does not exist, the project is not manual, the folder path is
///   blank/invalid, or the new path is already registered to a different project.
/// </exception>
/// <exception cref="DirectoryNotFoundException"> The new folder does not exist. </exception>
void ChangeProjectPath(string projectId, string newFolderPath);
```

Validation:
- **projectId must exist** and have `Source == ProjectSource.Manual`. Discovered projects cannot be changed; raise `ArgumentException`.
- **newFolderPath** must pass the same validation as `AddProject`: non-blank, fully qualified existing directory, normalized via `ProjectPathPolicy.Normalize`.
- **Deduplication:** normalize the new path and check `projectState.Projects` with `ProjectPathPolicy.AreEqual`. If a match is found:
  - Same project ID → no-op return (the project already points to that normalized path; this is not an error).
  - Different project ID → `ArgumentException("The selected folder is already registered to a different project.")`.

Mutation:
- Update `project.Path` to the normalized new path.
- Re-derive `project.Name` from the folder name using the same logic as `CreateProject` (`:168` and `:175-178` in `ProjectService.cs`).
- Call `this.logoResolver.Remove(projectId)` to invalidate the logo cache entry.
- Preserve `Id`, `Source`, `IsFavorite`, and `LastUsed` unchanged.
- If the project is the current selection (`projectState.CurrentProjectId == projectId`), it remains selected.
- Call `this.Persist()` (atomic config write + `ProjectsChanged` event).

Rationale: the validation mirrors `AddProject` and reuses `ProjectPathPolicy` for normalization and comparison. The same-project no-op avoids redundant persistence and event raises. Logo invalidation is required because the folder's logo file may have changed. The name is always re-derived to keep it consistent with the folder; no separate rename feature is in scope.

## Decision 2: UI placement — "Change folder…" in the row action panel

The new button appears in the `SpacedPanel` at `:171-201` in `SettingsProjectRegistryPage.xaml`, **before** the "Open project folder" button. Order: "Change folder…" → "Open project folder" → "Remove project". The button is visible only when `IsManual` is true (bind `Visibility` to a new `CanChangePath` property on `ProjectModel`, or directly to `IsManual` via `BoolToVisibility` converter).

Icon: use `IconGlyphs.Edit` (pencil) so the action is visually distinct from the adjacent "Open project folder" (`IconGlyphs.FolderOpen`) button. Tooltip and accessible name: "Change project folder".

Rationale: grouping path-related actions (change, open) before the destructive remove action follows the existing left-to-right action severity pattern. Making the button visible only for manual projects enforces the read-only contract for discovered projects at the UI layer, reinforcing the service-level guard.

## Decision 3: Command and error flow

`ProjectRegistryViewModel` adds:

```csharp
public RelayCommand<ProjectModel> ChangePathCommand { get; }
```

Constructor:
```csharp
this.ChangePathCommand = new RelayCommand<ProjectModel>(this.ChangePath, item => item.IsManual);
```

Method:
```csharp
private void ChangePath(ProjectModel item)
{
    if (item is null || !item.IsManual)
    {
        return;
    }

    this.ErrorText = null;

    var folder = this.dialogs.SelectFolder("Change Project Folder");
    if (string.IsNullOrWhiteSpace(folder))
    {
        return; // user canceled
    }

    try
    {
        this.projects.ChangeProjectPath(item.Id, folder);
        this.Load(); // re-enrich
    }
    catch (DirectoryNotFoundException ex)
    {
        this.ErrorText = $"The selected folder does not exist: {ex.Message}";
    }
    catch (ArgumentException ex)
    {
        this.ErrorText = $"Could not change project path: {ex.Message}";
    }
    catch (Exception ex)
    {
        this.ErrorText = $"Could not change project path: {ex.Message}";
    }
}
```

Validation and error text follow the existing patterns at `:172-215` and `:234-257`. The service's validation errors are caught and displayed in the error line; the user remains on the page with the error visible.

After a successful change, `this.Load()` (`:123-141`) re-enriches all models through the catalog, triggering `ProjectModelListSynchronizer.Synchronize` (`:138`), which updates the changed project's row in place. The row's name, path, and logo path reflect the new folder immediately.

Rationale: the command structure mirrors `RemoveCommand` and `OpenFolderCommand` (same `RelayCommand<ProjectModel>` pattern, same guard check, same error-handling shape). The `Load()` call ensures the UI reflects the mutation without requiring a separate manual refresh.

## Decision 4: Validation contract — same project vs different project

When the normalized new path matches an existing project via `ProjectPathPolicy.AreEqual`:
- **Same project ID:** no-op return. The project already points to that path (potentially via a non-normalized input that normalized to the same result). This is **not** an error; the service simply returns without raising an event or persisting.
- **Different project ID:** throw `ArgumentException("The selected folder is already registered to a different project.")`. The UI catches this and displays the error text.

Rationale: treating the same-project case as a no-op avoids redundant config writes and `ProjectsChanged` events for idempotent calls. The user may have re-selected the project's current folder by accident; rejecting this as an error would be hostile. The different-project case must reject because the configuration document does not support multiple projects pointing to the same path (the deduplication contract established by `AddProject` `:85-89`).

## Decision 5: Name re-derivation — always from the folder name

`ChangeProjectPath` re-derives the display name using the same logic as `CreateProject` (`:168`, `:175-178`): `Path.GetFileName(normalized.TrimEnd(...))`, falling back to the full normalized path if empty. There is no separate "rename" parameter or editable name field in this change.

Rationale: keeping the name synchronized with the folder name maintains consistency with the `AddProject` contract. A separate rename feature (allowing the user to decouple the display name from the folder name) is out of scope for this change.

## Decision 6: Logo cache invalidation — per project ID

The service calls `this.logoResolver.Remove(projectId)` (same as `RemoveProject` `:193`) after updating the path. The logo cache entries are keyed by project ID and persisted in `logos.json`. Removing the entry ensures the next enrichment re-scans the new folder for a logo file.

The enrichment flow (triggered by `ProjectsChanged` → `Load()`) calls `ProjectEnricher.Enrich`, which reads through `ProjectLogoResolver` (`:132` in `ProjectRegistryViewModel.cs`). The resolver's persistent cache respects invalidation, so the logo path in the enriched snapshot reflects the new folder.

Rationale: the logo may differ between the old and new folders, so unconditional invalidation is correct. Re-using the `Remove(id)` method (already tested and correct) avoids duplicating cache-clearing logic.

## Decision 7: Persistence and event contract

The service calls `this.Persist()` (`:255-259`) after applying the path/name changes and logo invalidation. This:
- Writes the configuration atomically via `projectStateStore.Update(state => this.projectState.CopyTo(state))`.
- Raises `ProjectsChanged` event.
- Every UI surface observing `ProjectsChanged` (launch window, Settings registry) receives the notification and refreshes.

Same-project no-op path: when the normalized new path equals the project's current path (same project ID), the method returns early **before** calling `Persist()` and raising the event. No side effects occur.

Rationale: immediate atomic persistence matches the existing mutation contract (`AddProject`, `RemoveProject`, `ToggleFavorite` all persist immediately). The no-op path avoids unnecessary config writes for idempotent calls.

## Decision 8: Current selection preservation

If the changed project is the current selection (`projectState.CurrentProjectId == projectId`), it remains selected after the path change. The `projectState.CurrentProjectId` field is not cleared or modified.

Rationale: the project's identity (ID) is unchanged, so its selection status should survive the path correction. Clearing the selection would force the user to re-select the project after correcting its path, which is unnecessary friction.

## Decision 9: Staging — single atomic implementation

No staging is required. The change is implemented in one pass:
1. Add the service API with validation, mutation, and tests.
2. Add the view-model command and error handling with tests.
3. Add the XAML button (manual rows only).
4. Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and manual smoke.

The change is small enough and its dependencies (enrichment, `ProjectPathPolicy`, logo invalidation, persistence) are already stable and tested, so a multi-stage migration is not justified.

Rationale: the mutation fits the existing service/enrichment/UI pattern without requiring intermediate compatibility layers. One atomic implementation reduces overhead and maintains clarity.

## Open questions

None. All decisions are grounded in the existing codebase contracts and verified file references.

## Alternatives considered and rejected

### Alternative 1: Allow renaming discovered projects
**Rejected.** Discovered projects are managed by Project Scanning (`ReconcileDiscoveredProjects` adds/removes them based on scanning results). Allowing manual path correction for discovered projects would break the scanning contract — the next scan would remove the project if its original path is no longer discovered, or create a duplicate if the new path is discovered independently. The Settings page already documents that discovered projects are read-only (`:199-203` in `openspec/specs/project-management/spec.md`).

### Alternative 2: Separate rename and relocate operations
**Rejected.** The name is always derived from the folder name (`CreateProject` `:168`). Introducing a separate "rename" feature that decouples the display name from the folder name would require a schema change (persisting a user-editable name distinct from the folder-derived name), migration, and UI for manual name editing. This is out of scope for a path correction feature. The current design keeps the name synchronized with the folder, which is simpler and consistent with the existing project model.

### Alternative 3: Allow correcting the path of a missing folder to another missing folder
**Rejected.** The validation requires `Directory.Exists(normalized)` (same as `AddProject` `:80-83`). Allowing a user to point a project to a non-existent folder would leave the project in the same broken state (missing folder chip visible) and provide no value. The feature is for **correcting** a path to a folder that exists but was moved/renamed, not for speculative path registration.

### Alternative 4: Warn if the new folder lacks a logo file
**Rejected.** Logo resolution already handles missing logo files gracefully (falls back to `LogoOptions.DefaultLogoPath`, verified at `unify-logo-pipeline` and `introduce-project-model`). Warning the user about a missing logo would add UI complexity for a non-critical concern. The enrichment flow re-scans the new folder and applies the default logo if needed; no additional warning is required.

## Deviations from brief

None. The design follows the brief's fixed scope: manual projects only, path and name update, validation mirroring `AddProject`, logo cache invalidation, immediate persistence, no schema change, and Settings-page-only UI.
