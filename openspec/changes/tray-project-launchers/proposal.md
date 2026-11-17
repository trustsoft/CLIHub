## Why

The tray context menu today exposes three separate, partly overlapping entry points:

- **Recent Projects** — a flat list of the ten most recently used projects; selecting one only changes
  the current project (`src/CLIHub/Tray/TrayMenuBuilder.cs:28-44` at the plan commit `4c9efa0`).
- **Launch Agent** — a submenu of every catalog plugin that declares a `launch` command, listing them
  against the *current* project only (`TrayMenuBuilder.cs:45,107-130` at `4c9efa0`). It gates only on
  `plugin.Commands?.Launch is not null` (`src/CLIHub/Tray/TrayStateProjection.cs:52` at `4c9efa0`) — it
  does not check that the agent is installed on the host or used in the project, and it collapses to
  `(select a project)` when no project is current.
- **Add Project…** — opens the folder workflow (`TrayMenuBuilder.cs:47-49` at `4c9efa0`).

To launch agent A in project B the user must first pick B under Recent Projects, then reopen the menu
and pick A under Launch Agent — two menu steps with a hidden coupling to the current project, and a
Launch Agent list that offers agents irrelevant to the project. The launch window already solves this
with a project-scoped, detection-filtered agent list; the tray does not.

This change replaces the three items with a single project-first structure: each favorite project is a
submenu of its initialized agents, and picking an agent makes that project current and launches the
agent in one action.

The first implementation of this structure was reviewed and judged too heavy: it repeated the
favorite marker and a `Run ` verb on every row, offered a redundant `Show CLIHub` item next to the tray
icon's own click behavior, and spread `What's New` and the update item over two top-level rows. The
confirmed redesign keeps the project-first structure and lightens the menu as described under
"What Changes" and in "Revision" below.

## What Changes

The tray context menu becomes, top to bottom:

1. **Header** (disabled) — `Current: <name>` or `No project selected`. Unchanged.
2. **One submenu per favorite project** (`IsFavorite == true`), in storage order from
   `IProjectService.GetAllProjects()`, labelled with the **project name only** (no ` *` suffix) and
   carrying a `FavoriteStar` icon. When no project is a favorite, one disabled item
   (`(no favorite projects)`). The **Recent Projects** submenu is removed.
3. **Per project submenu items** — the agents *initialized for that project*: a plugin that exposes a
   launch command **and** `IAgentDetectionService.IsInstalledInSystem(plugin)` **and**
   `IAgentDetectionService.IsAvailableInProject(plugin, project.Path)`. Each is a launch action
   (`AgentCommandKind.Launch`) labelled with the **agent name only** (no `Run ` prefix) and carrying a
   `Play` icon.
4. **Default agent first** — in every project submenu, when `AppPreferences.DefaultAgentId` resolves to
   a plugin in the catalog, that agent is the first item (even when it fails the initialization checks),
   followed by a `Separator`. No duplication: an initialized default appears exactly once, first, with
   the separator after it. When no default is configured or the id is not in the catalog, there is no
   default item and no separator. Remaining items are the other initialized agents. An empty submenu
   shows one disabled item (`(no agents)`).
5. **Add Project…** — unchanged behavior, `FolderAdd` icon, placed directly below the project list and
   before `Settings`.
6. **Settings** (`Settings` icon) and **`Updates`** (`Sync` icon) — `Updates` contains, in order, the
   dynamic update item (`Check for updates` / `Checking for updates…` / `Downloading update…` /
   `Download <version> and restart`, with `Sync` or `Update` as its icon) and `What's New` (`Page` icon).
   The submenu label is `Updates` only — the platform draws the submenu arrow itself.
7. **Separator, Exit** (`Exit` icon) — the separator keeps separating the action rows from `Exit`; the
   **Show CLIHub** item is removed because left-clicking the tray icon and the global hotkey already
   show the launch window.
8. **Command surface trimmed** — `TrayMenuCommands` keeps only the slots the builder binds:
   `SelectProjectAndLaunchAgentAsync`, `AddProject`, `ShowSettings`, `ShowReleaseNotes`,
   `CheckForUpdates`, `RequestUpdateDownload`, `Exit`.

Every actionable item shows a `Segoe MDL2 Assets` glyph in `MenuItem.Icon`; the header, the disabled
placeholders, and separators show none.

Launching an agent from project P's submenu first calls `IProjectService.SetCurrentProject(P.Id)` and
then executes the shared agent workflow with P, so the launch target and the application's current
project agree.

The tray state snapshot is reshaped (per-project initialized-agent lists, a resolved default plugin,
favorite projects) and stays WPF-free; `TrayMenuBuilder` keeps all WPF types, including the glyph
helper. The menu is refreshed when it opens so newly favorited projects, a changed default agent, and
fresh detection are reflected without a restart.

## Capabilities

### New capability: `tray-menu`

The tray context menu has no durable spec today. This change adds
`openspec/specs/tray-menu/spec.md` with the menu's structure, the project-submenu composition and
favorite ordering, the default-agent rule, the label and icon rules, the `Updates` grouping, the empty
states, and the launch-makes-project-current behavior.

`skip_specs` is **not** set: the tray menu is observable user behavior and gets a new delta spec
(`specs/tray-menu/spec.md`, ADDED requirements).

## Impact

- `src/CLIHub/Tray/TrayMenuState.cs` — reshape the state and command records; `TrayMenuCommands` shrinks
  from ten slots to seven.
- `src/CLIHub/Tray/TrayStateProjection.cs` — compose per-project agent lists, resolve the default
  plugin, project favorite projects; register the detection and preference dependencies; drop the
  `SetCurrentProject` pass-through that the removed `SelectProject` slot used.
- `src/CLIHub/Tray/TrayCommandHandlers.cs` — the project-scoped launch runs the shared agent workflow and
  the select-project side effect; `CreateCommands` loses its `selectProject` parameter; the private
  `LaunchAgentAsync(Plugin, Project)` stays as the shared launch step.
- `src/CLIHub/Tray/TrayMenuBuilder.cs` — build the new menu shape, the label changes, the `Updates`
  submenu, the `Show CLIHub` removal, and the glyph helper.
- `src/CLIHub/Tray/TrayIconController.cs` — refresh the menu when it opens; the command `with` mutation
  now overrides only `RequestUpdateDownload` and `Exit`.
- `src/CLIHub/Tray/TrayActions.cs`, `src/CLIHub/Tray/ITrayActions.cs` — pass the command set through
  without the removed select slot.
- `src/CLIHub/Composition/ServiceRegistration.cs` — the projection gains `IAgentDetectionService` and
  `IPreferencesStore` (both already registered in Core DI).
- `tests/CLIHub.Tests/Services/TrayActionsTests.cs` — state-shape assertions, the new handler test, and
  the reduced inert-slot test; the removed `LaunchAgentAsync` slot test goes away.
- `tests/CLIHub.Tests/ViewModels/Updates/SharedUpdatePresentationTests.cs` — `CreateProjection` gains
  the new dependencies.
- New tray-menu behavior tests (`tests/CLIHub.Tests/Services/TrayMenuBuilderTests.cs`) using `RunOnSta`,
  covering labels, the `Updates` group, icons, and the tail order.

## Non-Goals

- Not changing the launch window, its agent list, or `AgentListComposer` semantics.
- Not adding a change event to `IPreferencesStore` or to detection.
- Not persisting a new menu-order preference or making the tray project list sorted by recency; order
  stays `GetAllProjects()` storage order.
- Not changing update checking, the update states themselves, the What's New content, Settings, the
  current-project header, or Exit behavior.
- Not introducing a custom `ContextMenu` template, theme-aware icon colors, or a new glyph set; the
  menu keeps the default WPF menu look and the existing `IconGlyphs` constants.
- Not introducing a second default-agent rule for the launch window.

## Dependencies

None. The change is self-contained inside the tray stack plus its tests; every injected service
(`IProjectService`, `IPluginCatalog`, `IUpdateWorkflow`, `IAgentDetectionService`, `IPreferencesStore`,
`IAgentCommandWorkflow`) is already registered.

## Revision (2026-11-17)

This proposal supersedes the first version of the change (base plan commit `4c9efa0`) and the
uncommitted first implementation of it. The revision:

- removes `Show CLIHub` (left-click and the global hotkey already show the window) and keeps the
  separator only before `Exit`;
- groups the update surfaces under `Updates`: the dynamic update item followed by `What's New`;
- drops the ` *` favorite suffix and the `Run ` action prefix so items are labelled with the project or
  agent name only;
- puts an MDL2 glyph in `MenuItem.Icon` on every actionable item, using `IconGlyphs` constants through a
  builder-local `TextBlock` helper with a hard-coded icon font;
- removes the now-unbound `SelectProject`, `LaunchAgentAsync`, and `ShowLaunchWindow` command slots and
  the wiring and test surface that existed only for them.
