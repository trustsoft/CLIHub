# Tasks: introduce-project-model

Implementation happens in a fresh session with no prior context; `design.md` carries the full
rationale and file references. Tasks are checked off as they land. Stages are sequential — each one
ends buildable and green; never start the next stage with a red tree.

**Prerequisite:** `unify-logo-pipeline` is implemented first. This change consumes its seams and
carries its deferred tail (task 4.x).

## 1. Stage 1 — Core enricher, catalog, launch window

- [ ] 1.1 Verify the prerequisite seams exist: `LogoOptions` registered in Core, public
      `ProjectLogoResolver` applying the `?? DefaultLogoPath` fallback at resolver level,
      `InvalidateAll(string? keyPrefix)` + `LogoCacheKeys`, `PathToImage` converter backed by
      `LogoImageSourceCache`. If any is missing, stop — `unify-logo-pipeline` is not done
- [ ] 1.2 Core: add `EnrichedProject` (plain record: `Project Project`, `bool FolderExists`,
      `string? LogoPath`) and `ProjectEnricher` (`public sealed`, `CLIHub.Core.Projects`, depends on
      `ProjectLogoResolver`): `Enrich(IReadOnlyList<Project>)` → one `Directory.Exists` per project
      + resolver lookup, XML docs; register as a singleton in `AddProjectServices`
      (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`)
- [ ] 1.3 Core tests: enricher with existing folder / missing folder / logo fallback to
      `LogoOptions.DefaultLogoPath` / repeat call served from the persistent cache without a folder
      rescan
- [ ] 1.4 App: add `ProjectModel` (`src/CLIHub/ViewModels/Projects/`): INPC row with identity
      (`Id`, `Name`, `Path`), enriched values (`LogoPath` final string, `IsFolderAvailable`,
      `IsFavorite`), derived fields (`SourceMark`, `SourceLabel`, `DisplayPath`, `CanRemove` ⇔
      manual, `CanOpen` ⇔ folder available), an `Update(...)` method for in-place refresh, and **no
      commands**; add `ProjectModelCatalog`: stable instance per project Id, Id-keyed update,
      drops vanished Ids (pattern: `AgentItem.Update` + `AgentListSynchronizer`); unit tests for
      diff/update/instance stability
- [ ] 1.5 `ProjectPaneController`: `Projects` → `ObservableCollection<ProjectModel>`; `Refresh()`
      reads all projects, enriches via `ProjectEnricher`, syncs the catalog, keeps the surface
      collection ordered without recreating rows, and restores selection by Id; project mutations
      (`AddProject`, remove, toggle favorite) still go through `IProjectService` by Id
- [ ] 1.6 `LaunchWindowViewModel`: `SelectedProject` → `ProjectModel?`; `RefreshAgents()` uses
      `SelectedProject?.Path`; launch context for `WindowActionCoordinator` resolves the selected
      model's `Id` through `IProjectService` at command time (no shared live instance); `Projects`
      exposes the model collection
- [ ] 1.7 Enrichment triggers: refresh on `ProjectsChanged` (existing path) plus re-enrichment when
      the launch window is shown or activated (`LaunchWindow.ShowOnPointerMonitor` / `Activated`)
- [ ] 1.8 `LaunchWindow.xaml`: project-row `DataTemplate` binds `ProjectModel` — `LogoPath` stays a
      string binding through `PathToImage`; `Name`, `Path`, `IsFavorite` bindings unchanged
- [ ] 1.9 Rewrite `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs` and
      `ProjectPaneControllerTests` toward catalog/enricher fakes; `dotnet build CLIHub.sln` and
      `dotnet test CLIHub.sln` green
- [ ] 1.10 Manual smoke: launch-window rows render names, final logos, and the favorite star;
      selection survives a refresh; removing the selected project updates the pane

## 2. Stage 2 — Settings registry on the same models

- [ ] 2.1 `ProjectRegistryViewModel`: `Projects` → `ObservableCollection<ProjectModel>` fed from the
      shared catalog; `Load()` = re-enrich + in-place sync (no Clear + re-add); delete the per-row
      `Directory.Exists` (the enricher owns folder checks); counts (`ManualCount`,
      `DiscoveredCount`, `MissingFolderCount`) and summaries derive from the collection
- [ ] 2.2 Commands: `RemoveCommand`/`OpenFolderCommand` become `RelayCommand<ProjectModel>` gated by
      `CanRemove`/`CanOpen`; Settings page `Load()` is the re-enrichment trigger (already wired to
      `ProjectsChanged` — keep)
- [ ] 2.3 Delete `ProjectRegistryItem`; update `IProjectRegistrySection` (collection element type,
      command parameter types); update `SettingsProjectRegistryPage.xaml` bindings (availability chip
      and remove-button visibility per the model's properties) and `SettingsViewModel` usage;
      `ServiceRegistration` shape unchanged (transient)
- [ ] 2.4 Rewrite `ProjectRegistryViewModelTests` toward catalog/enricher fakes; `dotnet build
      CLIHub.sln` and `dotnet test CLIHub.sln` green
- [ ] 2.5 Manual smoke: registry rows, provenance marks, missing-folder chip, add/remove/open
      actions; a project change made in the launch window updates the registry in place with no
      visible rebuild

## 3. Stage 3 — `Project` becomes a pure record; detached reads (breaking)

- [ ] 3.1 Stage gate — search the app project for residual `Project` bindings/uses on registered
      surfaces (`LaunchWindow`, Settings pages, tray-adjacent views): none may bind `Project`
      records anymore. The unregistered legacy `MainWindow` is a documented exception (see
      `design.md`)
- [ ] 3.2 Strip `INotifyPropertyChanged` from `Project`
      (`src/CLIHub.Core/Models/Projects/Project.cs`): plain auto-properties, remove the
      notification plumbing and usings; `LogoPath` stays on the record (persisted config field)
- [ ] 3.3 `IProjectService` read methods (`GetAllProjects`, `GetRecentProjects`, `GetCurrentProject`)
      return detached copies — new `Project` items each call, never the internal instances (reuse
      the `ConfigurationSnapshot.CloneProject` shape or add a copy factory); document the copy
      semantics in the interface XML docs
- [ ] 3.4 Core tests: copy semantics — mutating a returned project changes neither later reads nor
      persisted state; update any test broken by the INPC removal
- [ ] 3.5 `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` green; grep gate:
      `INotifyPropertyChanged` no longer referenced by `Project`, and no registered UI binds
      `Project` records

## 4. Stage 4 — inherited tail: no hidden logo mutation in `ProjectService`

- [ ] 4.1 `ProjectService`: remove `RefreshLogos()` and its calls, the `GetCurrentProject` logo
      write, and the `CreateProject` initial `LogoPath` write — logo resolution happens only via the
      enricher path; keep `logoResolver.Remove(projectId)` on removal/reconcile (cache eviction)
      and decide the fate of the test-only internal `ResolveLogo` (delete it and move those tests to
      the resolver, per `design.md` open question 5)
- [ ] 4.2 Update `ProjectServiceTests` (logo-related setups move to the enricher/resolver tests);
      `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` green
- [ ] 4.3 Manual smoke: fresh start renders every project logo via the enricher; deleting a project
      folder and re-showing the launch window flags availability at the next trigger; `%APPDATA%`
      `config.json` may carry a vestigial `logoPath` value — accepted, see `design.md`

## 5. Docs and final validation

- [ ] 5.1 `docs/glossary.md`: add the Project (persisted record) vs ProjectModel (enriched display
      row) terminology from the proposal
- [ ] 5.2 Update the relevant architecture topic (project/scanning or UI page) with the enrichment
      flow: enricher in Core, catalog in the app, snapshot triggers — keep it brief and point to
      this change's design for rationale
- [ ] 5.3 Final `dotnet build CLIHub.sln` with no new warnings and `dotnet test CLIHub.sln` green
- [ ] 5.4 `openspec validate --all` passes
