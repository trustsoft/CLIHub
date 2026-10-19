# Harden agent/plugin boundaries: detached plugin reads, agent row fidelity, card update gating

Applies the agent-domain verdict of the architecture assessment: close the detached-reads gap plus
targeted micro-fixes. This is explicitly **not** a transplant of the project
catalog/enricher pattern (`ProjectModelCatalog` + `ProjectEnricher`); the agent side keeps its
`AgentItem` + `AgentListSynchronizer` shape.

## Why

All findings below are verified in code:

- **Leaked live plugin instances.** `IPluginCatalog.GetAllPlugins()` returns the catalog's internal
  list and its live items (`src/CLIHub.Core/Plugins/PluginCatalog.cs:118-122`). Callers can mutate a
  returned plugin (or hold it across async work) and observe or corrupt catalog state. The project
  side closed the identical gap with detached reads; the plugin side still leaks.
- **Agent rows do not announce capability changes.** `AgentItem.Update`
  (`src/CLIHub/ViewModels/Agents/AgentItem.cs:113-125`) swaps `Plugin`, `Name`, `LogoPath`, and
  `IsAvailable`, but never raises `CanLaunch`/`CanResume`/`CanUpdate`/`CanShowVersion`. Today this is
  unobservable because command availability rides `CommandManager.RequerySuggested`; the first XAML
  binding of those flags would show stale buttons.
- **The launch window refreshes projects but not agents on show/activate.**
  `LaunchWindow.ShowOnPointerMonitor` and `OnWindowActivated`
  (`src/CLIHub/Views/LaunchWindow.xaml.cs:62-93`) call only `viewModel.RefreshProjects()`. The agent
  pane (installation state, project availability, versions) stays at whatever the last refresh
  produced, although `LaunchWindowViewModel.RefreshAgents()` exists and version re-population is
  cache-served (`AgentVersionService.DefaultTtl` = 15 minutes).
- **Agent card Update availability ignores running agents.** The card Update button binds
  `IsEnabled="{Binding CanUpdate}"` (`src/CLIHub/Views/SettingsAgentsPage.xaml:183`) and
  `AgentManagementViewModel.Load()` sets `CanUpdate` purely from the declared command
  (`AgentManagementViewModel.cs:176`). The `agent-ui` requirement "Update blocked by a running agent"
  requires the action to be *unavailable* with the card explaining while a matching process runs. The
  execution-time half exists (`AgentManagementService.UpdateAsync` blocks and reports,
  `src/CLIHub.Core/Agents/AgentManagementService.cs:69-76`); the availability half is missing. The
  pane-level path (`WindowActionCoordinator.CanExecuteUpdate`/`ExecuteUpdate`) has both halves.
- **Frozen `Status` property.** `AgentItem.Status` (`AgentItem.cs:39-43`) is `init`-only, never
  updated by `Update`, and bound only by the legacy `MainWindow.xaml:118` — the documented
  build-only exception.

## What Changes

- **Stage 1 — Core: detached plugin reads.** Add a `Plugin.Copy()` factory (deep-copying
  `Commands` and `Detection`); `IPluginCatalog.GetAllPlugins()` returns copies of the loaded
  snapshot; copy semantics documented on the interface. Consumers that hold a plugin across async
  operations (`AgentPaneController.PopulateVersionsAsync`, `LaunchCommandCoordinator`) only read
  identity and commands, so copies are safe — same argument as the project change's design.
  Id-comparison semantics documented per layer (no behavior change).
- **Stage 2 — App: agent row fidelity and launch-window refresh.** `AgentItem.Update` is
  restructured into a single `Apply` path that also raises the four `Can*` notifications when the
  plugin reference changes, with a guard comment preserving explicit property names in `SetValue`
  call sites; `Status` is kept but documented as legacy-window-only and frozen. The launch window
  calls `RefreshAgents()` after `RefreshProjects()` on show and on activation.
- **Stage 3 — App: card Update availability gating.** `AgentManagementViewModel` takes the
  `AgentProcessMonitor` and computes card `CanUpdate` at load time as "declares the update command
  AND no matching agent process is running", setting the card message to explain a block. The
  execution-time block stays where it is (`AgentManagementService`); the view model does not
  duplicate it. This stage implements the existing `agent-ui` requirement — no spec delta.
- **Staged migration (3 stages, see `tasks.md`):** each stage ends buildable and green.

## Impact

- Affected specs:
  - `plugin-seeding` — ADDED: *Detached plugin reads*.
  - `agent-availability-display` — MODIFIED: *Agent row properties update in place* (capability
    flags follow the refreshed plugin); ADDED: *Agent pane refreshes when the launch window is shown
    or activated*.
  - `agent-ui` — no delta: Stage 3 brings the code into compliance with the existing
    *Agent card action availability* requirement.
- Affected code (future):
  - `src/CLIHub.Core/Models/Plugins/Plugin.cs` — `Copy()` factory
  - `src/CLIHub.Core/Plugins/PluginCatalog.cs` + `IPluginCatalog.cs` — detached reads, docs
  - `src/CLIHub/ViewModels/Agents/AgentItem.cs` — `Apply` path, `Can*` notifications, `Status` note
  - `src/CLIHub/Views/LaunchWindow.xaml.cs` — show/activate agent refresh
  - `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — process-monitor-gated `CanUpdate`
  - `tests/CLIHub.Core.Tests/Plugins/PluginCatalogTests.cs`,
    `tests/CLIHub.Tests/ViewModels/Agents/AgentItemTests.cs`,
    `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`
- Consumers verified read-only (compile and behave unchanged with copies): `AgentProcessMonitor`,
  `ProjectScanningService`, `TrayStateProjection`, `AgentPaneController`/`AgentListComposer`,
  `AgentManagementViewModel`, legacy `MainWindow`.

## Non-goals

Consciously declined by the assessment; revisit only when a trigger appears:

- **No `AgentModelCatalog` / `EnrichedAgent` snapshot enricher.** The project change earned its
  enricher because two surfaces (launch pane + Settings registry) shared enriched rows. The agent
  side has one live consumer of pane rows (`AgentPaneController`); a catalog/enricher pair would be
  machinery without a second consumer. Revisit if a second live consumer of agent rows appears.
- **No shared generic base classes** over Project/Agent models, catalogs, or synchronizers. The two
  domains differ in identity semantics, refresh triggers, and enrichment inputs; a generic
  abstraction would couple them for roughly two implementations.
- **No merging `AgentCardItem` into `AgentItem`.** The card and the pane row carry different state
  and lifetimes (page rebuild vs stable row identity).
- **No settings in-place cards.** `AgentManagementViewModel.Load()` rebuilds cards, losing per-card
  `Message`. Low severity: `Load` is infrequent (page navigation) and messages are per-action
  feedback. Deferred.
- **No comparer behavior changes.** Id-comparison differences between layers are documented and
  pinned by tests, not "fixed" (see `design.md`).
- **No work in the legacy `MainWindow`** — the documented build-only exception.
- **No live process monitoring on the Settings page.** Card availability is a snapshot at `Load()`,
  consistent with the rest of the application's snapshot refresh triggers.
- **No DI regrouping** — `AgentProcessMonitor` is already a Core-registered singleton; the new
  constructor dependency resolves without registration changes.
