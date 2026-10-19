# Design: harden-agent-plugin-boundaries

## Current state (verified in code)

| Concern | Today | File |
|---|---|---|
| Plugin reads leak | `GetAllPlugins()` returns `this.plugins` — the live internal list with live items; items are also mutated by the loader itself (`plugin.PluginDirectory = …`, `plugin.LogoPath = …`) | `src/CLIHub.Core/Plugins/PluginCatalog.cs:57-122` |
| Copy precedent | `Project.Copy()` exists for exactly this purpose on the project side; `ConfigurationSnapshot.CloneProject` is the older shape | `src/CLIHub.Core/Models/Projects/Project.cs:54-63`, `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs:59` |
| Plugin graph children | `Plugin` holds mutable `AgentCommands` (four nullable `PluginCommand` slots) and `AgentDetection` (two `List<string>`); scalars are strings | `src/CLIHub.Core/Models/Agents/AgentCommands.cs`, `src/CLIHub.Core/Models/Agents/AgentDetection.cs`, `src/CLIHub.Core/Models/Plugins/PluginCommand.cs` |
| Catalog consumers | `AgentProcessMonitor.GetRunningAgents`, `ProjectScanningService` (passes plugins into `RepositoryDiscovery.Enumerate` on a worker thread), `TrayStateProjection`, `AgentPaneController` → `AgentListComposer`, `AgentManagementViewModel.Load`, legacy `MainWindow` — all read-only; no reference-equality or in-place-mutation reliance anywhere (searched) | across `src/` |
| Async holders | `AgentPaneController.PopulateVersionsAsync` holds `item.Plugin` across `GetVersionAsync`; `LaunchCommandCoordinator.ExecuteAsync` holds `item.Plugin` across `agentCommandWorkflow.ExecuteAsync` — both read identity/commands only | `src/CLIHub/ViewModels/Agents/AgentPaneController.cs:159-188`, `src/CLIHub/ViewModels/Launch/LaunchCommandCoordinator.cs:69-120` |
| Row update path | `AgentItem.Update` uses `SetValue(ref field, value, nameof(…))` with **explicit** names; `Can*` are computed properties over `Plugin` and never re-announced; `Status` is `init`-only and untouched by `Update` | `src/CLIHub/ViewModels/Agents/AgentItem.cs:39-43,63-81,113-125` |
| Row creation | `AgentListSynchronizer` creates rows via object initializer (`new AgentItem { Plugin = …, … }`), which requires accessible `init` accessors | `src/CLIHub/ViewModels/Agents/AgentListSynchronizer.cs:41-47` |
| Window triggers | `ShowOnPointerMonitor` → `RefreshProjects()` + `ApplyDefaultAgentSelection()`; `OnWindowActivated` → `RefreshProjects()` only | `src/CLIHub/Views/LaunchWindow.xaml.cs:62-93` |
| Card update availability | `Load()` sets `card.CanUpdate` from the declared command only; button binds `IsEnabled="{Binding CanUpdate}"`; the view model has no process monitor | `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:58-59,173-188`, `src/CLIHub/Views/SettingsAgentsPage.xaml:178-189` |
| Card update execution | `AgentManagementService.UpdateAsync` blocks with `AgentUpdateOutcome.Blocked` + message when a matching process runs; the view model surfaces `result.Message` on the card | `src/CLIHub.Core/Agents/AgentManagementService.cs:57-91`, `AgentManagementViewModel.cs:282-291` |
| Pane update gating (reference pattern) | `CanExecuteUpdate` gates availability on `processMonitor.HasRunningAgents(agent.Plugin)`; `ExecuteUpdate` double-checks at execution time and reports blocked agents | `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:145-182` |
| Comparers | `AgentListSynchronizer`: `OrdinalIgnoreCase` (row identity, selection restore); `PluginCatalog` duplicate detection: ordinal `==` on descriptor ids; `ProjectModelCatalog`: `Ordinal` | `AgentListSynchronizer.cs:24,34,76`, `PluginCatalog.cs:139-147`, `ProjectModelCatalog.cs:14,32` |
| Version cache | `AgentVersionService.DefaultTtl` = 15 min (preference-overridable); refresh-triggered population is cache-served within the TTL | `src/CLIHub.Core/Agents/AgentVersionService.cs:21,55,109-115` |
| DI | `AgentProcessMonitor` registered as a Core singleton; `AgentManagementViewModel` registered transient as `IAgentManagementSection` — a new ctor dependency resolves unchanged | `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:115`, `src/CLIHub/Composition/ServiceRegistration.cs:159` |

## Stage 3 verdict: the gating claim, verified and narrowed

The assessment flagged that the card Update "appears to lack the `AgentProcessMonitor` gating".
Verification against the current code and both governing requirements:

- **Execution-time gating: PRESENT.** `2026-10-19-per-agent-update-blocking` implemented the
  Settings-path check in `AgentManagementService.UpdateAsync` (`GetRunningAgents(plugin)` →
  `AgentUpdateOutcome.Blocked` + "Update blocked while the agent is running: …"), and the view model
  writes that message onto the card. This satisfies `preferences-ui` → *Agent card actions* →
  "Update blocked while running" (the update does not start; the card reports blocked).
- **Availability gating: ABSENT.** `agent-ui` → *Agent card action availability* → "Update blocked by
  a running agent" requires that while a matching process runs, "Update" **is unavailable** and the
  card **explains** the block. The card's `CanUpdate` is computed at `Load()` from the declared
  command only; the button stays enabled while the agent runs. The pane path has both halves
  (`WindowActionCoordinator.CanExecuteUpdate` + execution double-check).

So the stage **survives but narrows**: only the availability half is missing. Consequences for the
fix shape:

- Add `AgentProcessMonitor` to `AgentManagementViewModel`; at `Load()` compute
  `card.CanUpdate = declaresUpdate && !processMonitor.HasRunningAgents(plugin)` and, when blocked,
  set `card.Message` to an explaining text (wording aligned with the service's message).
- **No execution-time double check in the view model.** The service already owns it
  (`preferences-ui` "Update blocked while running" passes today); duplicating it in the VM would add
  a second authority for the same rule. The pane coordinator keeps its own double-check because it
  bypasses `AgentManagementService` and talks to `IAgentCommandWorkflow` directly.
- **Snapshot semantics:** availability reflects the process state at `Load()`; the Settings page's
  `Load()` is the refresh trigger, as for every other card field. No live process watching.
- **No spec delta.** Both requirements already exist with exactly the target wording; this stage is
  compliance. Precedent: `2026-10-20-modularize-di-registration` (no `specs/` directory).

## Decision 1: `Plugin.Copy()` with deep `Commands`/`Detection`

A shallow copy would share `AgentCommands` and `AgentDetection`, so a caller could still mutate the
catalog's command set or detection markers through a "detached" plugin — the leak would survive at
one indirection. The copy factory clones:

- scalars (`Id`, `Name`, `Description`, `LogoPath`, `PluginDirectory`) — strings, assign;
- `Commands` — new `AgentCommands` with a new `PluginCommand` per defined kind (`Launch`, `Resume`,
  `Version`, `Update` — via `Get(AgentCommandKind)` so a kind added later cannot be missed);
- `Detection` — new `AgentDetection` with copied `SystemPaths` / `ProjectIndicators` lists.

XML docs mirror `Project.Copy()` ("detached copy; callers can mutate a returned plugin without
corrupting the loaded catalog"). `GetAllPlugins()` returns `this.plugins.Select(p => p.Copy()).ToArray()`.

Copies are safe for every consumer: all reads only (verified, table above); the two async holders
read identity/commands across awaits — a detached copy is exactly what makes that hold safe, the same
argument the project change used for `WindowActionCoordinator`'s launch context. No code anywhere
relies on `Plugin` reference identity (searched: only Id-based comparisons exist). Existing test
mocks that return shared `Plugin` instances (e.g. `AgentManagementViewModelTests.CreateViewModel`)
keep working: the view models only read.

## Decision 2: `AgentItem` — one `Apply` path, explicit names preserved, `Can*` announced

Restructure `Update(Plugin, string, string?, bool)` toward the `ProjectModel.Apply(snapshot)` shape:

- `Update` validates nothing new and delegates to a **single private `Apply`** — the only
  post-construction mutation path, so a field added to the row has exactly one place to be wired,
  which prevents the "required init property silently freezes" class (how `Status` died).
- `Apply` assigns through the existing explicit-name `SetValue` and, when the `plugin` reference
  changed, raises `CanLaunch`, `CanResume`, `CanUpdate`, `CanShowVersion`. After Stage 1 every
  refresh supplies a fresh copy, so reference change is the correct and cheap trigger; the four
  notifications per refreshed row are negligible.
- **The trap, encoded:** `SetValue`'s `propertyName` argument stays explicit at every call site, and
  `Apply`/`SetValue` carry a guard comment: do **not** "simplify" this to a
  `[CallerMemberName]`-attributed helper while call sites pass through `Apply`/`Update` — the
  compiler would then happily raise `"Update"`/`"Apply"` as the property name and silently kill every
  row notification. (`ProjectModel` can afford `CallerMemberName` because `Apply` assigns through the
  properties themselves; `AgentItem` keeps `required init` accessors — see below — so `Apply` writes
  fields and must name them.)
- **Why not full `ProjectModel` shape (`private set` properties + property-assigning `Apply`):**
  `AgentItem` is created via object initializer in `AgentListSynchronizer`
  (`new AgentItem { Plugin = … }`), which needs accessible `init` accessors; switching to `private
  set` breaks that, and switching to a constructor changes public surface for no behavioral gain.
  Keep `required init`; keep the single `Apply` with explicit names.
- **`Status` (fate):** keep, with a comment: bound only by the legacy `MainWindow.xaml:118`
  (documented build-only exception), intentionally not wired into `Apply`, frozen at construction on
  launch-window rows. Deleting it would require editing the legacy window — out of scope by policy;
  "fixing" the legacy window likewise.

Tests extend `AgentItemTests`: plugin swap raises the four `Can*` (command set differs), no
capability notifications when nothing changed, existing property notifications preserved.

## Decision 3: launch-window show/activate refreshes agents too

Mirror of the project change's task 1.7 (enrichment triggers on show/activate):

- `ShowOnPointerMonitor`: `RefreshProjects()` → `RefreshAgents()` → `ApplyDefaultAgentSelection()`.
  The default-agent lookup matches rows by plugin Id, so it must run against freshly synchronized
  rows — that ordering is part of the fix.
- `OnWindowActivated`: `RefreshProjects()` → `RefreshAgents()`.

Cost: `AgentPaneController.Refresh` recomposes rows (Id-keyed, in place) and restarts version
population, which is served by the version cache within the 15-minute TTL — trivial. Both entry
points call the existing `LaunchWindowViewModel.RefreshAgents()`; no new code path. Selection and
ordering behavior is already specified (`agent-availability-display`: stable row identity, selection
survival, in-place updates) and unchanged.

## Decision 4: id comparers — document and pin, do not change

| Layer | Comparison | Verdict |
|---|---|---|
| `PluginCatalog` duplicate detection (`FindLoadedPluginDirectory`) | ordinal `==` on descriptor ids | Keep. Loader-level identity is the deterministic contract `plugin-seeding` already specifies (first valid plugin in deterministic directory order wins; ids are textual, matching subdirectory names). |
| `AgentListSynchronizer` row identity + selection restore | `OrdinalIgnoreCase` | Keep. Defensive display-level matching: Windows directory names cannot differ by case alone, so a case-variant id can only arise from descriptor edits; ignoring case there keeps one stable row instead of duplicating/flapping it. |
| `ProjectModelCatalog` | `Ordinal` | Keep. Project ids are service-generated stable keys, not user-visible text. |

The layers answer different questions (which descriptor wins at load vs which row is the same agent
on screen vs which record is this project). Stage 1 documents the catalog's ordinal semantics in
`IPluginCatalog` XML docs and pins duplicate detection with a test; no behavior change.

## Decision 5: staging

Three stages, each ending buildable and green. Order is load-bearing:

1. **Core detached reads first.** Every later stage reasons about copies (`Can*` raise on reference
   change; cards hold detached plugins); the copies must exist before anything depends on them.
2. **Agent row fidelity + window refresh second.** Pure app-side hygiene on top of Stage 1;
   independent of Stage 3.
3. **Card availability gating last.** Behavioral fix, verified against the spec first; independent
   of Stages 1–2 except that cards now hold copies.

## Spec deltas — placement rationale

- **`plugin-seeding`, ADDED *Detached plugin reads*.** `plugin-seeding` is the de-facto plugin
  catalog capability: it already owns deterministic loading, duplicate-id resolution, manual reload,
  and reload notifications, even though its Purpose sentence is seeding-focused. A new capability for
  one read-semantics requirement would fragment the catalog contract. Mirrors the project change's
  ADDED *Detached project reads*.
- **`agent-availability-display`, MODIFIED *Agent row properties update in place*.** That requirement
  is exactly about rows reflecting refreshed state through property notifications; the modified text
  adds the command-capability clause and one scenario. **ADDED *Agent pane refreshes when the launch
  window is shown or activated*** — this capability owns the agent-pane refresh family (stable row
  identity, selection survival, in-place updates, refresh re-evaluating installation); the trigger
  requirement completes that family symmetrically to `project-management`'s "Snapshot refresh
  triggers".
- **`agent-ui`, no delta.** Stage 3 implements the existing *Agent card action availability*
  requirement (see the verdict section). `preferences-ui` needs no delta either — its *Update blocked
  while running* scenario already passes and is untouched.

## Consequences worth recording

- **`AgentItem.Plugin` and `AgentCardItem.Plugin` become detached copies.** Nothing compared them by
  reference before; nothing may start doing so (noted as a review rule for Stage 1).
- **The card Update button can now be disabled by process state**, a visible change on the Settings
  CLI Agents page: with the agent running, Update grays out and the card explains. Stopping the agent
  and reloading the page re-enables it (snapshot semantics — documented in the proposal's non-goals).
- **`PluginCatalog` allocation per read.** `GetAllPlugins()` now allocates one array + one `Plugin`
  graph per call. Call sites are user-interaction-rate (refresh, show/activate, page load, tray
  build) — negligible; not worth a caching layer.
- **Legacy `MainWindow` compiles unchanged** against copies (it only reads), consistent with its
  documented build-only exception.

## Open questions for the implementation session

1. Blocked-card message wording: reuse the service's exact phrasing
   ("Update blocked while the agent is running") or a shorter availability-oriented variant? Lean:
   reuse, one authority per sentence.
2. Should `TestAsync`/`UpdateAsync` outcomes re-evaluate `card.CanUpdate` (an update command may have
   just spawned the agent)? Lean: no — the next page `Load()` is the trigger; revisit only if the
   stale window is observable in practice.
3. `AgentItem.Apply` parameter list vs a small snapshot record (`AgentRowSnapshot`): the four-parameter
   `Update` signature is unchanged either way; pick when writing `AgentItemTests` first. Lean: keep
   parameters — a record would invite the enricher-shaped machinery the assessment declined.
4. Whether the duplicate-id ordinal pin belongs in `PluginCatalogTests` as a new test or an
   assertion added to an existing duplicate test; follow the file's current style.
