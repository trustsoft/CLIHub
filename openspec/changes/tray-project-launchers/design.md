## Context

See `proposal.md` — Why and What Changes.

### Verified current state (2026-11-17)

**`TrayMenuBuilder.Build` (`src/CLIHub/Tray/TrayMenuBuilder.cs:16-76`)** builds, in order: header item
(`:22-26`), **Recent Projects** submenu (`:28-44`), `BuildLaunchAgentMenu` (`:45`, body `:107-130`),
**Add Project...** (`:47-49`), **Settings** (`:51-53`), **What's New** (`:55-57`),
`BuildUpdateItem` (`:59-63`, body `:78-105`), `Separator` (`:65`), **Show CLIHub** (`:67-69`), **Exit**
(`:71-73`). Project labels use `project.IsFavorite ? $"{project.Name} *" : project.Name`
(`:38`); the Recent Projects item's click only calls `commands.SelectProject(id)` (`:39`). The launch
agent item's click is `async (_, _) => await commands.LaunchAgentAsync(plugin, state.CurrentProject)`
(`:125`).

**`TrayMenuState` / `TrayMenuCommands` (`src/CLIHub/Tray/TrayMenuState.cs:14-19,33-41`)** are immutable
records of `Project?`/`Plugin` Core models and delegates. Slots:
`SelectProject: Action<string>`, `LaunchAgentAsync: Func<Plugin, Project, Task>`, `AddProject`,
`ShowSettings`, `ShowReleaseNotes`, `CheckForUpdates`, `RequestUpdateDownload`, `ShowLaunchWindow`,
`Exit`.

**`TrayStateProjection` (`src/CLIHub/Tray/TrayStateProjection.cs`)** takes `IProjectService`,
`IPluginCatalog`, `IUpdateWorkflow` (`:28-39`); `GetState()` (`:49-55`) uses
`this.projects.GetRecentProjects(10)` and `this.pluginCatalog.GetAllPlugins().Where(plugin => plugin.Commands?.Launch is not null)`.
`SetCurrentProject(projectId)` (`:61`) is the public select action. It subscribes to
`ProjectsChanged`, `PluginsChanged`, `UpdateStateChanged` (`:36-38`).

**`TrayCommandHandlers` (`src/CLIHub/Tray/TrayCommandHandlers.cs`)** — `CreateCommands(selectProject)`
(`:61-70`) passes `this.LaunchAgentAsync` into the `LaunchAgentAsync` slot. `LaunchAgentAsync`
(`:91-98`) only runs
`IAgentCommandWorkflow.ExecuteAsync(plugin, project, AgentCommandKind.Launch)` and warns on failure; it
never calls `SetCurrentProject`. The update-download, show-launch-window, and exit slots are inert
defaults the controller replaces (`:68-70`).

**`TrayActions` (`src/CLIHub/Tray/TrayActions.cs:23-29`)** wires
`this.Commands = this.handlers.CreateCommands(this.projection.SetCurrentProject)` — so `SelectProject`
already routes to `IProjectService.SetCurrentProject`.

**`TrayIconController` (`src/CLIHub/Tray/TrayIconController.cs:126-135`)** — `RefreshMenu()` rebuilds the
menu from `this.actions.GetState()` and a `with`-mutated command set (replacing only
`RequestUpdateDownload`, `ShowLaunchWindow`, `Exit`). `OnStateChanged` (`:146-155`) marshals to the
dispatcher and calls `RefreshMenu`. The controller owns the `ContextMenu` assigned at `:134`; there is no
`Opened` handler today (the only `ContextMenu` usages in `src/` are in these two tray files).

**`IProjectService` (`src/CLIHub.Core/Projects/IProjectService.cs:19,25,31,77`)** — `GetAllProjects()`
returns detached copies in storage order; `GetRecentProjects(limit)` orders by `LastUsed` descending
(`src/CLIHub.Core/Projects/ProjectService.cs:43-55`); `GetCurrentProject()` returns a copy;
`SetCurrentProject(projectId)` sets the id, stamps `LastUsed`, persists, and publishes `ProjectSelected`
when the selection changed (`ProjectService.cs:252-271`).

**`IAgentDetectionService` (`src/CLIHub.Core/Agents/IAgentDetectionService.cs`) / `AgentDetectionService`
(`src/CLIHub.Core/Agents/AgentDetectionService.cs`)** — `IsInstalledInSystem` (`:30`) and
`IsAvailableInProject` (`:34`) are cached file-system checks keyed `sys:<id>` and `proj:<id>:<path>`
(`:57-62`) with a TTL from `AgentProbePolicy.ResolveTtl` (`:77`), default 15 minutes
(`AgentProbePolicy.cs:12,22`). `IsAvailableInProject` returns false for a blank or non-existent project
folder (`:100-103`).

**`AgentListComposer.Compose` (`src/CLIHub.Core/Agents/AgentListComposer.cs:16-28`)** is the existing
installed+project filter: `IsInstalledInSystem` is a hard gate (uninstalled agents are skipped), then
`IsAvailableInProject` dims/filters by project. The tray composition reuses the same two predicates.

**`ProjectListOrdering.FavoritesFirst` (`src/CLIHub.Core/Projects/ProjectListOrdering.cs:18`)** — favorites
first preserving source order. The requirement here is a **filter** (favorites only) in storage order, so
this helper is not needed.

**`AppPreferences.DefaultAgentId` (`src/CLIHub.Core/Models/Configuration/AppConfig.cs:141`)** — `string?`,
"ID of the agent the launch window preselects, or null for none". `IPreferencesStore`
(`src/CLIHub.Core/Configuration/IPreferencesStore.cs`) exposes `Load()` and `Update(...)` only — **no
change event**.

**Core DI (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:69,130`)** already registers
`IPreferencesStore` and `IAgentDetectionService` (constructed with `IPreferencesStore`). **App DI
(`src/CLIHub/Composition/ServiceRegistration.cs:126-134`)** registers the tray stack as singletons; the
projection's constructor is resolved, so adding dependencies flows through automatically.

**Tests.** `tests/CLIHub.Tests/Services/TrayActionsTests.cs` asserts the current state shape
(`GetState_ProjectsCurrentRecentLaunchableAndUpdateState:17-40`), the workflow delegation
(`LaunchAgentCommand_UsesSharedAgentWorkflow:43-62`), the controller-owned inert slots
(`CreateCommands_ControllerOwnedSlots_AreInertDefaults:108-131`), and disposal. `CreateActions:133-151`
constructs `TrayStateProjection(projects, catalog, updates)`.
`tests/CLIHub.Tests/ViewModels/Updates/SharedUpdatePresentationTests.cs:145-152` constructs
`TrayStateProjection` in `CreateProjection`. `tests/CLIHub.Tests/Services/TrayMenuBuilderRegistrationTests.cs`
covers DI only. There are **no** `TrayMenuBuilder` behavior tests. `tests/CLIHub.Tests` has `UseWPF=true`,
and `tests/CLIHub.Tests/Support/TestHelpers.cs:26` provides `RunOnSta(Action)`.

### Spec mapping

No `tray`/`tray-menu` capability exists in `openspec/specs/` (closest: `project-management`,
`agent-detection`, `agent-commands`). This change adds `specs/tray-menu/spec.md` with ADDED requirements
only; no MODIFIED requirement and therefore no scenario-name preservation concern. `skip_specs` stays
unset.

## Decisions

### Decision 1: New immutable, WPF-free tray state shape (DECIDED)

Replace `TrayMenuState` with:

```csharp
public sealed record TrayProjectAgents(Project Project,
                                       Plugin? DefaultAgent,
                                       IReadOnlyList<Plugin> InitializedAgents);

public sealed record TrayMenuState(Project? CurrentProject,
                                   IReadOnlyList<TrayProjectAgents> FavoriteProjects,
                                   string? AvailableUpdateVersion,
                                   bool IsDownloadingUpdate,
                                   bool IsCheckingForUpdates);
```

`TrayProjectAgents` carries the `Project`, the **resolved** default `Plugin?` (already catalog-checked),
and the initialized agents for that project. `TrayMenuState` drops `RecentProjects` and
`LaunchableAgents`.

**Rationale:** the menu needs "per project: default + initialized agents", so the projection resolves the
default and filters agents once and the builder stays a dumb renderer. Keeping only `Project`/`Plugin`
Core models in the record keeps the WPF dependency out of the state, matching the current design and the
Core/app boundary.

**Alternative — keep a flat `LaunchableAgents` list plus a map of project→agents (rejected):** forces the
builder to re-derive the default and cross-reference projects, duplicating projection logic in the view.

**Alternative — reuse the launch window's `AgentListEntry` (rejected):** it carries dimming/availability
presentation state the tray does not need, and it lives in an app-project namespace oriented to the launch
window.

### Decision 2: New `SelectProjectAndLaunchAgentAsync(projectId, plugin)` command slot (DECIDED)

Extend `TrayMenuCommands` with a **new** slot and keep `SelectProject`, `LaunchAgentAsync`, and every
other slot unchanged:

```csharp
public sealed record TrayMenuCommands(Action<string> SelectProject,
                                      Func<Plugin, Project, Task> LaunchAgentAsync,
                                      Func<string, Plugin, Task> SelectProjectAndLaunchAgentAsync,
                                      Action AddProject, /* ...unchanged... */);
```

`TrayMenuBuilder` binds each agent item to
`commands.SelectProjectAndLaunchAgentAsync(project.Id, plugin)`. In `TrayCommandHandlers`:

```csharp
private async Task SelectProjectAndLaunchAgentAsync(string projectId, Plugin plugin)
{
    this.projects.SetCurrentProject(projectId);
    var project = this.projects.GetCurrentProject();
    if (project is null || project.Id != projectId)
    {
        return;
    }
    await this.LaunchAgentAsync(plugin, project);
}
```

**Rationale:** `TrayMenuBuilder` is a pure projection with no business logic; making the project current
before running the workflow is an application rule, so it belongs in the handler. The handler reads the
current project back after the setter so the workflow runs against the persisted, stamped record rather
than a stale snapshot.

**Alternative — rely on the existing `SelectProject` + `LaunchAgentAsync` slots (rejected):** the builder
would have to fire `SelectProject(project.Id)` then `LaunchAgentAsync(plugin, project)`. The two slots
are resolved to different owners (`SelectProject` to `projection.SetCurrentProject`, `LaunchAgentAsync` to
the handler), so the ordering coupling would leak the "select first" rule into the view. One
project-scoped handler keeps the rule in one place and is independently testable.

**Alternative — change `LaunchAgentAsync`'s signature to take the project id (rejected):** would force the
controller's `with`-mutated command set and the existing test to change for no benefit.

`TrayIconController.RefreshMenu`'s `with` replacement (`:128-133`) touches only
`RequestUpdateDownload`, `ShowLaunchWindow`, `Exit`, so the new slot is copied through unchanged.

### Decision 3: Refresh menu content when the context menu opens (DECIDED)

There is exactly **one** `ContextMenu` instance for the tray icon's lifetime. The menu content is
repopulated in place on `Opened`; the instance is never swapped and the handler is never re-attached.

- `TrayMenuBuilder.Build(state, commands)` keeps its current contract: it creates and returns a populated
  `ContextMenu`. It is used **only to create the first instance**.
- `TrayMenuBuilder.Populate(ContextMenu menu, TrayMenuState state, TrayMenuCommands commands)` clears
  `menu.Items` and repopulates that existing instance. `Populate` must not replace the instance and must
  not attach or re-attach any event handler.
- `TrayIconController` creates the `ContextMenu` once via `Build`, assigns it to
  `this.taskbarIcon.ContextMenu`, and attaches `Opened += (_, _) => this.RefreshMenu();` **once**, guarded
  (for example, it is attached as part of the one-time creation so later refreshes cannot re-attach it).
- `RefreshMenu()` calls `this.menuBuilder.Populate(this.taskbarIcon.ContextMenu, this.actions.GetState(),
  commands)` on that same instance. `OnStateChanged` and the `Opened` refresh both call `RefreshMenu`, so
  the initial creation, the state-change rebuild, and the on-open rebuild share a single refresh path.
- Because `Populate` runs synchronously inside the `Opened` event on the same, already-assigned instance,
  it must not raise `Opened` again or mutate the handler set; it only replaces item content.

On open, `RefreshMenu` reads a **fresh** `GetState()` and rebuilds the content; detection results are
cached (up to the 15-minute probe TTL), so a refresh is a few dictionary lookups in the common case and
re-checks the file system only after the TTL or an `InvalidateFor` (e.g. a Detect action).

**Rationale:** `IPreferencesStore` has no change event, so a changed `DefaultAgentId`, a newly favorited
project, or a fresh detection result otherwise never reaches the snapshot — the menu is rebuilt only on
`ProjectsChanged`/`PluginsChanged`/update-state changes. `ToggleFavorite` does raise `ProjectsChanged`,
but opening-time refresh additionally picks up the default-agent change and TTL-expired detection with no
new subscription and no event in Core.

**Alternative — subscription-only refresh (rejected):** leaves a newly enabled default agent invisible
until an unrelated event, which the brief calls out as a defect to avoid.

**Alternative — event on `IPreferencesStore` (rejected):** a broader Core change touching every consumer;
out of scope.

**Alternative — keep `Build` returning a fresh menu and swap `ContextMenu` on every refresh (partially
rejected):** swapping the instance means the `Opened` handler must move to the new instance each time and
carries a re-entrancy risk during the `Opened` event. Populating the same instance is simpler and matches
how a WPF menu is normally refreshed.

### Decision 4: Detection cost and cache semantics (DECIDED — documented cost)

For each favorite project P and each plugin with a launch command, an agent is "initialized" when
`IsInstalledInSystem(plugin)` **and** `IsAvailableInProject(plugin, P.Path)`. Both are cached by
`AgentDetectionService` (`sys:<id>` and `proj:<id>:<path>`, TTL default 15 min,
`AgentProbePolicy.cs:12`), so a rebuild performs at most one file-system probe per (plugin, project) pair
per TTL window; a warm cache is dictionary lookups. The favorite-only restriction keeps this to
`favorites × launch-capable plugins`, bounded by realistic numbers (≈2–10 favorites, ≈2–10 agents →
≈4–100 checks worst case, usually a warm cache). `IsAvailableInProject` short-circuits on a missing folder
(`AgentDetectionService.cs:100-103`), so a missing project path costs nothing.

**Rationale:** the trade-off is accepted because favorites-only bounds the product; no throttling or
background pre-computation is needed.

**Alternative — compute only on `ProjectsChanged`/`PluginsChanged` (rejected):** combined with Decision 3
an open-time refresh is the mechanism that keeps detection fresh; pre-computing on events alone would
serve stale results within the TTL anyway.

### Decision 5: Default-agent fallback (DECIDED)

The projection resolves `DefaultAgentId` as follows:

- `DefaultAgentId` null/whitespace → no default item, no separator.
- `DefaultAgentId` set but **not present** in `GetAllPlugins()` → no default item, no separator (the id
  is silently ignored; no warning dialog, since the tray is a passive surface).
- `DefaultAgentId` set and present in the catalog → that plugin is the submenu's first item. The default
  need **not** expose a launch command; if it does not, its item is rendered disabled
  (`IsEnabled = false`) but still first, with the separator after it.

The catalog check uses the **unfiltered** `GetAllPlugins()` (not the launch-command filter), so a default
plugin that lacks a launch command still resolves and shows as a disabled first item rather than
vanishing.

**Rationale:** "always the first item, even when it fails the initialization checks" is the brief's
decision; the only question is the meaning of "not in the catalog", which the brief left to decide.
Ignoring an unknown id is consistent with the menu's other graceful fallbacks (`(none)`, `(no agents)`).
Rendering a non-launchable default as a disabled first item keeps it visibly "the configured default"
without offering a Run that cannot work.

### Decision 6: Label formats (DECIDED)

- **Project header:** keep the existing convention `project.IsFavorite ? $"{project.Name} *" : project.Name`
  (`TrayMenuBuilder.cs:38`). Because only favorites are shown, every project submenu header shows the
  trailing ` * `. (The brief offered "or an equivalent star"; reusing the existing convention avoids a new
  marker style.)
- **Agent item:** `Run {plugin.Name}` — states the action ("Run") before the agent name, mirroring the
  existing action-prefixed items (`Download … and restart`) and the launch window's action vocabulary.
  Chosen over the bare name (ambiguous — it could mean select) and over `Launch {Name}` (the window verb
  is "Run").
- **Empty states:** `(no favorite projects)` (whole list empty) and `(no agents)` (a project with no
  default and no initialized agents), disabled.
- Formatting lives entirely in `TrayMenuBuilder` (view); no strings in the projection.

### Decision 7: Where the WPF stays (DECIDED)

- `TrayMenuState`, `TrayProjectAgents`, `TrayMenuCommands`, `TrayStateProjection`, and
  `TrayCommandHandlers` stay free of `System.Windows.*`; the projection depends on `IAgentDetectionService`
  and `IPreferencesStore` (Core interfaces).
- `TrayMenuBuilder`, `TrayIconController`, and `TrayActions` remain the only WPF-aware tray types
  (they already reference `System.Windows.Controls` / `Application`).
- No `Core` reference to the app project or vice versa beyond the existing direction.

### Decision 8: Controller-refresh behavior preserved (DECIDED)

`TrayIconController.RefreshMenu` keeps building from `this.actions.GetState()` and overriding only its
three owned slots. The added `Opened` refresh calls the same method, so the initial build, the
state-change rebuild, and the on-open rebuild converge on one code path.

## Deviations

- **State shape.** The brief asked to decide the shape; the delivered shape (Decision 1) introduces a
  `TrayProjectAgents` record not named in the brief as an explicit requirement — it is the minimal
  structure that keeps the builder a pure renderer.
- **`LaunchAgentAsync` retained.** The brief asked whether a new `TrayMenuCommands` slot is needed or the
  existing `LaunchAgentAsync` plus a select side effect suffices. Decision 2 adds a new slot **and keeps**
  `LaunchAgentAsync` (the menu no longer binds it, but keeping it avoids churn in the existing test and the
  controller's command set). The implementation session may remove `LaunchAgentAsync` if it is confirmed
  dead; recorded as an open question.
- **Refresh mechanism.** The brief listed open-on-`Opened` repopulation as an option; it is adopted
  (Decision 3) rather than rejected, so the "staleness" limitation is **not** carried as a non-goal.

## Open Questions

1. **Should `TrayMenuCommands.LaunchAgentAsync` be removed?** After Decision 2 the tray builder no longer
   binds it. Keeping it means `LaunchAgentCommand_UsesSharedAgentWorkflow`
   (`TrayActionsTests.cs:43-62`) still passes unchanged; removing it is cleaner but touches that test and
   the record. Verdict: decide in the implementation session; the spec does not require the slot either
   way.
2. **Exact `Opened`-guard mechanism** — attach once via a bool in `RefreshMenu`, or attach on the
   initially built menu only. Either satisfies the requirement; the implementing session picks the simpler
   (the menu instance is stable because the same `ContextMenu` is repopulated).
3. **Disabled default item copy** — whether a default plugin that does not expose a launch command should
   read `Run <Name>` (disabled) or a distinct label. The spec fixes "disabled and first"; the copy is a
   builder detail.

## Consequences

- The tray menu becomes project-first: launching agent A in project B is one click-depth path
  (project submenu → agent), and the project is made current as a side effect.
- `Recent Projects` and `Launch Agent` disappear; the maximum top-level item count is now favorites + a
  fixed tail (Add Project, Settings, What's New, update, Separator, Show CLIHub, Exit).
- The projection does detection work when the menu opens; bounded by favorites × launch-capable plugins
  and mitigated by the probe cache.
- A changed `DefaultAgentId` or a new favorite is reflected the next time the menu opens, without a
  restart.
- Existing tests that assert `RecentProjects`/`LaunchableAgents` must be updated to the new shape
  (expected, mechanical).
