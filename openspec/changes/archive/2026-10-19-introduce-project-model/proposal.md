# Introduce ProjectModel: one enriched UI row model over a pure project record

> **Dependency:** this change MUST be implemented after `unify-logo-pipeline`. It consumes the seams
> that change creates (see below) and carries out its explicitly deferred tail (removing the hidden
> logo mutation from `ProjectService` read methods). Implementation order is strictly sequential:
> `unify-logo-pipeline` → `introduce-project-model`.

## Why

`Project` (`src/CLIHub.Core/Models/Projects/Project.cs`) currently mixes three roles in one object:
persisted record, bindable UI row (`INotifyPropertyChanged`), and partially enriched display state.
The consequences, verified in code:

- **Hidden enrichment side effects.** `ProjectService.GetAllProjects()`, `GetRecentProjects()`, and
  `GetCurrentProject()` mutate `project.LogoPath` as a side effect of reading
  (`RefreshLogos`, `src/CLIHub.Core/Projects/ProjectService.cs`). Reads are not reads. This was to
  be removed by `unify-logo-pipeline`, which deferred it because it needs the enrichment seam this
  change creates.
- **Leaked live instances.** `GetAllProjects()` returns `projectState.Projects.ToList()` — a copy of
  the list whose items are the service's live internal instances. UI code holds mutable service
  state; mutating a returned project corrupts service state.
- **Two divergent row shapes for the same data.** The launch pane binds live `Project` instances
  (instance-preserving diff in `ProjectPaneController.Refresh()`), while the Settings registry
  rebuilds `ProjectRegistryItem` rows from scratch on every `Load()` (Clear + re-add, one
  `Directory.Exists` per row in the view model). Enrichment and derived display fields
  (provenance mark, missing-folder state) are duplicated and inconsistent between surfaces.
- **Launch context rides the shared instance chain.** `WindowActionCoordinator` receives
  `Func<Project?>` backed by `projectPane.CurrentProject`, another live-instance read, and the agent
  pane refresh consumes `CurrentProject?.Path`.

## What Changes

- **Terminology.** *Project* is the persisted brief record (`Id`, `Name`, `Path`, `Source`,
  `IsFavorite`, `LastUsed`) — a plain POCO after this change. *ProjectModel* is the enriched runtime
  model for UI display: identity + enriched snapshot + derived display fields, raises
  property-change notifications, stable instance per project Id.
- **One `ProjectModel` for all surfaces** (`src/CLIHub`): an INPC row with derived display fields
  (`SourceMark`, `DisplayPath`, `CanRemove`, `CanOpen`) and **no commands**, shared through a
  `ProjectModelCatalog` that keeps a stable instance per project Id and updates models in place
  (pattern reference: `AgentItem` + `AgentListSynchronizer`). The launch pane and the Settings
  registry both display these instances; `ProjectRegistryItem` is deleted; `IProjectRegistrySection`
  changes signature (transient; single consumer `SettingsViewModel` + tests).
- **Commands stay on surface VMs.** View models own `RelayCommand`s and take `ProjectModel` as
  `CommandParameter`; the model carries only state and derived flags.
- **Core enrichment service** (`src/CLIHub.Core`): an explicit enricher produces plain enriched
  snapshots (folder existence + final logo path via the public `ProjectLogoResolver`, which already
  applies the `?? LogoOptions.DefaultLogoPath` fallback). Freshness is event-driven snapshot
  recompute at defined triggers — `ProjectsChanged`, launch window shown/activated, Settings page
  `Load()` — with no folder watchers and no timers.
- **Logo delivery stays string-based.** `ProjectModel.LogoPath` is the final path; XAML binds
  through the existing `PathToImage` converter backed by `LogoImageSourceCache`.
- **Staged migration (4 stages, see `tasks.md`):** launch window first, then the Settings registry,
  then stripping `INotifyPropertyChanged` from `Project` and making `IProjectService` reads return
  detached copies, and finally the inherited tail — removing the hidden logo mutation from
  `ProjectService` read methods.
- **Tray and Core scanning are untouched:** `TrayStateProjection`/`TrayCommandHandlers` and
  `ProjectScanningService` keep working with pure `Project` records.

## Impact

- Affected specs: `project-management` (ADDED: enriched project display models; detached project
  reads).
- Affected code (future):
  - `src/CLIHub.Core/Models/Projects/Project.cs` — strip `INotifyPropertyChanged` (Stage 3)
  - `src/CLIHub.Core/Projects/ProjectService.cs` + `IProjectService.cs` — detached reads (Stage 3),
    remove hidden logo mutation (Stage 4)
  - new `src/CLIHub.Core/Projects/` enricher + enriched-snapshot record (Stage 1), registered in
    `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`
  - `src/CLIHub/ViewModels/Projects/ProjectPaneController.cs` — binds `ObservableCollection<ProjectModel>`
  - `src/CLIHub/ViewModels/Projects/ProjectModel.cs` + `ProjectModelCatalog.cs` — new
  - `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs`,
    `ProjectRegistryItem.cs` (deleted), `IProjectRegistrySection.cs` — Stage 2
  - `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs`,
    `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs` — launch context via project Id
  - `src/CLIHub/Views/LaunchWindow.xaml`, `src/CLIHub/Views/SettingsProjectRegistryPage.xaml` —
    bind the model
  - `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs`, `ProjectPaneControllerTests`,
    `ProjectRegistryViewModelTests`, and new `CLIHub.Core.Tests` enricher/copy-semantics tests
- Non-goals:
  - no changes to the `config.json` or `logos.json` formats (the persisted per-project `logoPath`
    field becomes vestigial — see `design.md`)
  - no `ProjectService` behavior changes beyond what the staging requires
  - no DI module regrouping of `ServiceRegistration` (~50 registrations → modules like
    `AddLaunchWindowModule`) — that is a separate future change
  - no tray model adoption; the tray keeps consuming pure `Project` records
  - no per-folder watchers or background timers
  - no `ImageSource` on the model (possible future unification covering agents and projects together)
