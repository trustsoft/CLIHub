# Design: introduce-project-model

## Current state (verified in code)

| Concern | Today | File |
|---|---|---|
| Project record | `Project` implements `INotifyPropertyChanged`; `IsFavorite`, `LastUsed`, `LogoPath` raise change notifications; `LogoPath` is written by the service as a hidden side effect of reads | `src/CLIHub.Core/Models/Projects/Project.cs` |
| Hidden enrichment | `GetAllProjects`/`GetRecentProjects` call `RefreshLogos()`; `GetCurrentProject` resolves and writes `project.LogoPath`; `CreateProject` writes an initial `LogoPath` | `src/CLIHub.Core/Projects/ProjectService.cs` |
| Leaked instances | Read methods return `projectState.Projects.ToList()` — new list, same live `Project` items; `ProjectState.ApplyTo(AppConfig)` hands the live list to persistence | `src/CLIHub.Core/Projects/ProjectService.cs`, `src/CLIHub.Core/Models/Projects/ProjectState.cs` |
| Launch pane rows | `ObservableCollection<Project>`; `Refresh()` diffs by Id and preserves instances — it *depends* on shared live instances for logo updates; `CurrentProject` is a service read per access | `src/CLIHub/ViewModels/Projects/ProjectPaneController.cs` |
| Launch context | `LaunchWindowViewModel` hands `() => this.projectPane.CurrentProject` to `WindowActionCoordinator`; `RunAgentCommandAsync` passes the `Project` into `LaunchCommandCoordinator.RunAsync` → `IAgentCommandWorkflow.ExecuteAsync(plugin, project, kind)`; `RefreshAgents()` uses `projectPane.CurrentProject?.Path` | `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs`, `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs`, `src/CLIHub/ViewModels/Launch/LaunchCommandCoordinator.cs` |
| Settings registry rows | `ProjectRegistryItem` (frozen snapshot row: `Id`, `Name`, `Path`, `IsManual`, `IsFolderMissing`, `SourceMark`, `SourceLabel`); `Load()` does Clear + re-add and runs `Directory.Exists` per row in the view model; counts/summaries derive from the rebuilt collection | `src/CLIHub/ViewModels/Projects/ProjectRegistryItem.cs`, `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs` |
| Registry contract | `IProjectRegistrySection` exposes `ObservableCollection<ProjectRegistryItem>` and `RelayCommand<ProjectRegistryItem>` commands; registered transient, consumed only by `SettingsViewModel` | `src/CLIHub/ViewModels/Projects/IProjectRegistrySection.cs`, `src/CLIHub/Composition/ServiceRegistration.cs`, `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` |
| Tray | `TrayStateProjection.GetState()` reads `GetCurrentProject()` / `GetRecentProjects(10)`; `TrayCommandHandlers` launches with the `Project` record. Menu state, no list binding | `src/CLIHub/Tray/TrayStateProjection.cs`, `src/CLIHub/Tray/TrayCommandHandlers.cs` |
| Scanning | `ProjectScanningService` (Core) counts via `GetAllProjects()` and mutates via `AddDiscoveredProject`/`ReconcileDiscoveredProjects` | `src/CLIHub.Core/Scanning/ProjectScanningService.cs` |
| XAML bindings | Launch-window project rows bind `LogoPath` (ImageBrush via `PathToImage`), `Name`, `Path` (width-aware `PathDisplay` MultiBinding), `IsFavorite`; registry rows bind `Name`, `SourceMark`, `SourceLabel`, `IsFolderMissing`, `Path` and pass the row as `CommandParameter` | `src/CLIHub/Views/LaunchWindow.xaml`, `src/CLIHub/Views/SettingsProjectRegistryPage.xaml` |
| Legacy window | `MainWindow` is the unregistered legacy reference; its code-behind binds `GetAllProjects()` items directly (`SelectedItem is Project`) | `src/CLIHub/Views/MainWindow.xaml.cs` |
| Tests | `LaunchWindowViewModelBuilder` mocks `IProjectService`; `ProjectPaneControllerTests` and `ProjectRegistryViewModelTests` use strict mocks | `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs`, `tests/CLIHub.Tests/ViewModels/Projects/` |

## Dependency: `unify-logo-pipeline` comes first

This change is implemented strictly after `unify-logo-pipeline` and consumes four of its seams:

1. `LogoOptions { string? DefaultLogoPath }` registered once in Core — the single fallback source.
2. The public `ProjectLogoResolver` with resolver-level fallback: every path it returns is final
   (`resolve ?? default`), so consumers never re-apply `??`.
3. Scoped cache invalidation `InvalidateAll(string? keyPrefix)` + `LogoCacheKeys` prefixes — the
   enricher reads the cache without ever clearing foreign namespaces.
4. The DI-constructed XAML value converter registered under the `PathToImage` resource key, backed by
   `LogoImageSourceCache` (frozen `BitmapImage`, decode failure → default).

It also inherits the tail `unify-logo-pipeline/design.md` (Decision 5) explicitly deferred: removing
the hidden logo mutation from `ProjectService.GetAllProjects()` / `GetRecentProjects()` /
`GetCurrentProject()` — that removal needs the enrichment seam this change creates (Stage 4).

## Decision 1: commands stay out of the model

`ProjectModel` carries state and derived flags only: `CanRemove` (⇔ manual provenance) and
`CanOpen` (⇔ folder available). Surface view models own the commands and take `ProjectModel` as
`CommandParameter` — the launch window keeps its selection-based `RelayCommand`s
(`RemoveCommand`, `ToggleFavoriteCommand` acting on the selected model), the registry keeps its
parameterized `RelayCommand<ProjectModel>` commands.

Rationale: the same model instance is shared across surfaces with different action sets — the launch
window allows all actions on any row, while the Settings registry shows discovered projects
read-only. Commands embedded in the model would force surface-specific availability (or callbacks
into services) into a shared object, and would make the model factory an impure composition root.
The `AgentItem` precedent already follows this split: `AgentItem` exposes `CanLaunch`/`CanResume`
flags and `WindowActionCoordinator` owns `RelayCommand<AgentItem>` — same shape, so this is
consistent with the codebase.

## Decision 2: one `ProjectModel` for all surfaces

A shared `ProjectModelCatalog` (app project, `src/CLIHub/ViewModels/Projects/`) holds the stable
instances keyed by project Id:

- `Update`-style method applies a fresh enriched snapshot to the existing instance in place
  (property setters raise notifications), creates instances for new Ids, and drops instances whose
  Id vanished (pattern reference: `AgentItem.Update` + `AgentListSynchronizer` in
  `src/CLIHub/ViewModels/Agents/`).
- The catalog owns *instances*, not surface orderings. Each surface keeps its own ordered
  `ObservableCollection<ProjectModel>` referencing the shared instances (launch pane: service order;
  registry: manual-first, then by name — there is no single canonical ordering, so a master
  collection would not fit both). Surfaces use a small reorder/insert/remove sync step of the
  `AgentListSynchronizer` kind; no row is ever recreated on refresh.

Benefits:

- The launch pane no longer depends on shared live `Project` instances for updates — an enriched
  refresh flows through the model's `Update`, not through mutation of a service-owned object.
- The Settings registry stops Clear+rebuild: no row-state loss or flicker, and the per-row
  `Directory.Exists` in `ProjectRegistryViewModel` is deleted (the enricher owns folder checks).
- Counts (`ManualCount`, `DiscoveredCount`, `MissingFolderCount`) and summaries derive from the
  surface collection as today — but over stable models.

`ProjectRegistryItem` is deleted. `IProjectRegistrySection` changes signature (collection element
type and command parameter types). This is transient breakage: the interface has a single
production consumer (`SettingsViewModel`, registered transient in `ServiceRegistration`) plus tests.

The tray stays on pure `Project`: `TrayMenuState` is built from service reads and holds records, not
bound rows; adopting models there would add coupling for no display benefit. Core scanning likewise
keeps consuming `Project` records through `IProjectService` mutation APIs.

## Decision 3: logo delivery — final string through the existing converter

`ProjectModel.LogoPath` is the **final** path: the enricher obtains it from the public
`ProjectLogoResolver`, which already applies `?? LogoOptions.DefaultLogoPath` at resolver level.
XAML keeps binding `LogoPath` through the `PathToImage` converter (DI-constructed, backed by
`LogoImageSourceCache`): one frozen decode per distinct path, default image for null/missing/
undecodable paths. The model does no image work.

Exposing `ImageSource` on the model is explicitly out of scope — a possible future unification could
cover agents and projects together and delete the converter, but that direction is recorded in
`unify-logo-pipeline/design.md` only.

## Decision 4: enrichment freshness — event-driven snapshot recompute

Semantics are a **snapshot, not live monitoring**: no per-folder watchers, no timers.

Triggers (each recomputes the full snapshot set):

- `IProjectService.ProjectsChanged` — the controllers already refresh on it today.
- Launch window shown or activated (`ShowOnPointerMonitor` / window `Activated`) — folder state and
  logos are current each time the user actually looks at the window.
- Settings Projects page `Load()` — already the page's refresh entry point.

Cost per trigger: one `Directory.Exists` per project plus a logo read that hits the persistent
`logos.json` cache (negative caching respected — no folder rescan unless entries were invalidated).
That is the same order of work the current hidden `RefreshLogos` does on every service read, just
made explicit and trigger-bound instead of read-bound.

Enrichment inputs: folder availability + final logo path. Nothing else is freshness-sensitive:
identity and provenance are immutable once created, favorites/recency flow through `ProjectsChanged`
refreshes as today.

## Decision 5: staging

Four stages, each leaving the tree buildable and green. Order is load-bearing:

1. **Core enricher + catalog + launch window.** Add the Core enricher (plain enriched snapshot) and
   `ProjectModelCatalog`; switch `ProjectPaneController.Projects` to
   `ObservableCollection<ProjectModel>`, `LaunchWindowViewModel.SelectedProject` to `ProjectModel?`;
   resolve launch context via project Id → service lookup, not a shared live instance. This stage
   creates the enrichment seam everything else needs.
2. **Settings registry on the same models.** Switch `ProjectRegistryViewModel` to the catalog,
   delete `ProjectRegistryItem`, update `IProjectRegistrySection` + tests. Second because it reuses
   Stage 1's enricher and catalog unchanged.
3. **`Project` becomes a pure record.** Strip `INotifyPropertyChanged`; `IProjectService` read
   methods return copies, never live internal instances; the UI binds `ProjectModel` everywhere.
   Deliberately last of the code stages: it breaks every consumer that still expects live instances
   or INPC on `Project`, so it is only safe once no registered UI binds `Project` (verify by search
   as a stage gate).
4. **Inherited tail.** Remove the hidden logo mutation from `ProjectService` read methods; logo
   resolution happens only through the enricher path. Last because until Stage 3 the UI reads
   `Project.LogoPath` off service-returned records — removing the mutation earlier would blank
   logos.

## Decision 6: shared instances vs copies — who owns what

The layering story, end to end:

- **Service layer (Core)** hands out *detached copies* (Stage 3). Callers can do anything with a
  returned `Project` without corrupting service state; persistence stays behind
  `IProjectStateStore.Update`. This matches the spirit of the existing configuration boundary
  (`configuration-snapshot`: detached snapshots), extended to the service reads that bypass it.
- **Enricher (Core)** is stateless: `Project` records in, plain `EnrichedProject` snapshots out
  (`Project` + folder existence + final logo path). It owns no collection and no event.
- **Catalog (app)** is the *only* long-lived owner of row instances. All binding goes through its
  stable `ProjectModel` instances; nothing binds a service-returned record.
- **Mutation flow** (e.g. toggle favorite): surface command → view model takes `ProjectModel` →
  service call by `Id` → `ProjectsChanged` → controller re-reads (copies), enriches, `Update`s the
  catalog in place → INPC fires → both surfaces reflect the change without rebuilding.

No layer holds the same object in two roles: records are data, models are rows, and the only
shared-state channel between them is the Id-keyed `Update`.

## Architectural shape

Core (`src/CLIHub.Core`, stays WPF-free):

- `EnrichedProject` — plain record: `Project Project`, `bool FolderExists`, `string? LogoPath`
  (final path; exact naming is an implementation call).
- `ProjectEnricher` — `public sealed`, `CLIHub.Core.Projects`, depends on the public
  `ProjectLogoResolver`; `Enrich(IReadOnlyList<Project>)` → one `Directory.Exists` per project +
  resolver lookup. Registered as a singleton in `AddProjectServices`.

App (`src/CLIHub`):

- `ProjectModel` (`ViewModels/Projects/`) — INPC row: identity (`Id`, `Name`, `Path`),
  enriched values (`LogoPath` final string, `IsFolderAvailable`, `IsFavorite`), derived display
  fields (`SourceMark`, `SourceLabel`, `DisplayPath`, `CanRemove`, `CanOpen`), and an `Update(...)`
  method for in-place refresh.
- `ProjectModelCatalog` (same folder) — Id-keyed instances, `Update`-style sync, drops vanished Ids.
- `DisplayPath` exposes the full folder path for surfaces that show it untrimmed (registry rows and
  tooltips). The launch window keeps its width-aware `PathDisplay` MultiBinding over the model's
  `Path` — that trimming is live-layout work a model field cannot do.

Launch context after Stage 1:

- `LaunchWindowViewModel` derives context from the selected model: agent-pane refresh uses
  `SelectedProject?.Path`; `WindowActionCoordinator` receives a resolver that maps the selected
  model's `Id` to a `Project` via `IProjectService` (a lookup at command time — after Stage 3 it
  receives a copy, which is fine because the workflow only reads identity/path). The alternative —
  teaching `IAgentCommandWorkflow` to take id+path instead of `Project` — touches Core workflow
  signatures for no behavioral gain and is not part of this change.

## Consequences worth recording

- **Persisted `logoPath` becomes vestigial.** After Stage 4 nothing writes `Project.LogoPath`, so
  `config.json` carries `null` for freshly persisted projects and stale values for older ones —
  harmless, because nothing reads the field anymore (display goes through the enricher). The field
  stays because changing the config format is a non-goal; a future cleanup change may drop it.
- **Legacy `MainWindow` is a documented exception.** It is unregistered and compiles unchanged
  against copies; its rows will show no logos after Stage 4. Not updated by this change.
- **`ProjectPaneController.CurrentProject` shrinks.** With context resolved from the selected model,
  the per-access `GetCurrentProject()` reads reduce to selection restoration (model matching the
  current project Id) inside `Refresh()`.
- **Test strategy shifts from strict service mocks to catalog/enricher fakes.** `ProjectService`
  mocks that return shared `Project` instances keep "working" but stop representing Stage 3
  semantics, so the builders are rewritten toward a fake enricher + catalog in Stages 1–2.

## Spec deltas

- `project-management` — ADDED *Enriched project display models* (final logo path on the model,
  snapshot triggers, no live monitoring, one shared instance per project across surfaces) and
  *Detached project reads* (reads return copies; UI binds display models, not records). No existing
  requirement changes: registration, persistence, selection, recency, favorites, logo resolution,
  removal, provenance, and the registry page all behave exactly as specified today.

## Alternatives considered

- **Commands inside the model** — rejected: surface-specific action sets (launch allows everything,
  registry is read-only for discovered projects) would force per-surface configuration into one
  shared object, and the enricher/factory would need service dependencies.
- **Two model types (launch row vs registry row)** — rejected: that is today's split
  (`Project` vs `ProjectRegistryItem`) and it is exactly the duplication this change removes.
- **`ImageSource` on the model now** — rejected: the converter + `LogoImageSourceCache` already
  solve decode cost and fallback; a model-level image type is a cross-domain (agents + projects)
  decision left for a future change.
- **Live folder watchers for availability** — rejected: cost and complexity for a field that only
  matters when a surface is visible; snapshot-on-trigger matches every existing refresh pattern.
- **Catalog owning one canonical ordered collection** — rejected for now: launch wants service order
  and the registry wants manual-first-by-name; forcing one ordering contorts one of the surfaces.
  The catalog owns instances; surfaces own their orderings. Revisit if a third surface appears.

## Open questions for the implementation session

1. Catalog API shape: a single `Sync(IReadOnlyList<EnrichedProject>)` vs `Update` + explicit
   add/remove calls. Either works; pick when writing the tests first.
2. Whether `ProjectPaneController` keeps any `CurrentProject`-like accessor after Stage 1 or the
   view model fully owns context resolution (lean: remove it; `Refresh()` restores selection by Id).
3. Registry XAML binding names after `ProjectRegistryItem` dies: bind `IsFolderAvailable` inverted
   for the chip vs adding a convenience `IsFolderMissing` on the model. Minimal-churn choice at
   implementation time.
4. `AgentPaneController.Refresh(string? projectPath, ...)` stays path-based (Stage 1 passes
   `SelectedProject?.Path`); confirm no signature change is warranted.
5. Whether the internal `ProjectService.ResolveLogo(string)` (used only by tests) is deleted in
   Stage 4 or kept as a resolver pass-through; delete if the tests move to the resolver.
