## Why

The tray context menu today exposes three separate, partly overlapping entry points:

- **Recent Projects** — a flat list of the ten most recently used projects; selecting one only changes
  the current project (`src/CLIHub/Tray/TrayMenuBuilder.cs:28-44`).
- **Launch Agent** — a submenu of every catalog plugin that declares a `launch` command, listing them
  against the *current* project only (`TrayMenuBuilder.cs:45,107-130`). It gates only on
  `plugin.Commands?.Launch is not null` (`src/CLIHub/Tray/TrayStateProjection.cs:52`) — it does not check
  that the agent is installed on the host or used in the project, and it collapses to
  `(select a project)` when no project is current.
- **Add Project…** — opens the folder workflow (`TrayMenuBuilder.cs:47-49`).

To launch agent A in project B the user must first pick B under Recent Projects, then reopen the menu
and pick A under Launch Agent — two menu steps with a hidden coupling to the current project, and a
Launch Agent list that offers agents irrelevant to the project. The launch window already solves this
with a project-scoped, detection-filtered agent list; the tray does not.

This change replaces the three items with a single project-first structure: each favorite project is a
submenu of its initialized agents, and picking an agent makes that project current and launches the
agent in one action.

## What Changes

The tray context menu becomes, top to bottom:

1. **Header** (disabled) — `Current: <name>` or `No project selected`. Unchanged (`TrayMenuBuilder.cs:22-26`).
2. **One submenu per favorite project** (`IsFavorite == true`), in storage order from
   `IProjectService.GetAllProjects()`. The label keeps the existing favorite marker convention
   (`<Name> *`, `TrayMenuBuilder.cs:38`). When no project is a favorite, one disabled item
   (`(no favorite projects)`). The **Recent Projects** submenu is removed.
3. **Per project submenu items** — the agents *initialized for that project*: a plugin that exposes a
   launch command **and** `IAgentDetectionService.IsInstalledInSystem(plugin)` **and**
   `IAgentDetectionService.IsAvailableInProject(plugin, project.Path)`. Each is a Run action
   (`AgentCommandKind.Launch`) labelled `Run <AgentName>`.
4. **Default agent first** — in every project submenu, when `AppPreferences.DefaultAgentId` resolves to
   a plugin in the catalog, that agent is the first item (even when it fails the initialization checks),
   followed by a `Separator`. No duplication: an initialized default appears exactly once, first, with
   the separator after it. When no default is configured or the id is not in the catalog, there is no
   default item and no separator. Remaining items are the other initialized agents. An empty submenu
   shows one disabled item (`(no agents)`).
5. **Add Project…** — unchanged behavior, placed directly below the project list and before `Settings`
   (`TrayMenuBuilder.cs:47`).
6. **Settings, What's New, update item, Separator, Show CLIHub, Exit** — unchanged behavior and relative
   order (`TrayMenuBuilder.cs:51-73`). The **Launch Agent** submenu is removed.

Launching an agent from project P's submenu first calls `IProjectService.SetCurrentProject(P.Id)` and
then executes the shared agent workflow with P, so the launch target and the application's current
project agree.

The tray state snapshot is reshaped (per-project initialized-agent lists, a resolved default plugin,
favorite projects) and stays WPF-free; `TrayMenuBuilder` keeps all WPF types. The menu is refreshed when
it opens so newly favorited projects, a changed default agent, and fresh detection are reflected without
a restart.

## Capabilities

### New capability: `tray-menu`

The tray context menu has no durable spec today. This change adds
`openspec/specs/tray-menu/spec.md` with the menu's structure, the project-submenu composition and
favorite ordering, the default-agent rule, the empty states, and the launch-makes-project-current
behavior.

`skip_specs` is **not** set: the tray menu is observable user behavior and gets a new delta spec
(`specs/tray-menu/spec.md`, ADDED requirements).

## Impact

- `src/CLIHub/Tray/TrayMenuState.cs` — reshape the state and command records.
- `src/CLIHub/Tray/TrayStateProjection.cs` — compose per-project agent lists, resolve the default
  plugin, project favorite projects; register the detection dependency.
- `src/CLIHub/Tray/TrayCommandHandlers.cs` — the project-scoped launch runs the shared agent workflow
  and the select-project side effect pipeline.
- `src/CLIHub/Tray/TrayMenuBuilder.cs` — build the new menu shape; remove recent/launch-agent menus.
- `src/CLIHub/Tray/TrayIconController.cs` — refresh the menu when it opens.
- `src/CLIHub/Tray/TrayActions.cs`, `src/CLIHub/Tray/ITrayActions.cs` — pass the project-aware select
  action through.
- `src/CLIHub/Composition/ServiceRegistration.cs` — the projection gains `IAgentDetectionService` and
  `IPreferencesStore` (both already registered in Core DI).
- `tests/CLIHub.Tests/Services/TrayActionsTests.cs` — state-shape assertions and the new handler test.
- `tests/CLIHub.Tests/ViewModels/Updates/SharedUpdatePresentationTests.cs` — `CreateProjection` gains
  the new dependencies.
- New tray-menu behavior tests (`tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs`) using `RunOnSta`.

## Non-Goals

- Not changing the launch window, its agent list, or `AgentListComposer` semantics.
- Not adding a change event to `IPreferencesStore` or to detection.
- Not persisting a new menu-order preference or making the tray project list sorted by recency; order
  stays `GetAllProjects()` storage order.
- Not changing the header, update, Settings, What's New, Show CLIHub, or Exit behavior.
- Not introducing a second default-agent rule for the launch window.

## Dependencies

None. The change is self-contained inside the tray stack plus its tests; every injected service
(`IProjectService`, `IPluginCatalog`, `IUpdateWorkflow`, `IAgentDetectionService`, `IPreferencesStore`,
`IAgentCommandWorkflow`) is already registered.
