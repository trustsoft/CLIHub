# tray-menu Specification

## Purpose
Defines the tray icon's context menu: the current-project header, one submenu per favorite project
with that project's initialized agents and the configured default agent first, the Add Project,
Settings, Updates, and Exit actions, and the menu's refresh-on-open and Windows-themed rendering
behavior.

## Requirements

### Requirement: Tray menu structure

The tray context menu SHALL show, top to bottom: a disabled current-project header; one submenu per
favorite project; `Add Project…`; `Settings`; the `Updates` submenu; a separator; and `Exit`. The
submenu's label is `Updates`; the arrow the platform draws for an item with children is not part of the
label. The menu SHALL NOT contain a `Show CLIHub` item, because the launch window is already reachable
by left-clicking the tray icon and by the global hotkey.

#### Scenario: Header reflects the current project

- **WHEN** a project is current
- **THEN** the first item is a disabled `Current: <name>` header

#### Scenario: Header when no project is selected

- **WHEN** no project is current
- **THEN** the first item is a disabled `No project selected` header

#### Scenario: Tail order is Settings, Updates, separator, Exit

- **WHEN** the menu is shown
- **THEN** `Settings`, the `Updates` submenu, a separator, and `Exit` appear below `Add Project…` in
  that order

#### Scenario: Recent Projects and Launch Agent are gone

- **WHEN** the menu is shown
- **THEN** no `Recent Projects` submenu, no `Launch Agent` submenu, and no `Show CLIHub` item are present

### Requirement: Favorite project submenus

The tray context menu SHALL show one submenu per project whose `IsFavorite` is true, in the storage order
returned by `GetAllProjects()`, and SHALL show no project submenu for a project that is not a favorite.

#### Scenario: Only favorites are shown

- **WHEN** the registered projects include favorites and non-favorites
- **THEN** exactly the favorites appear as top-level submenus

#### Scenario: Favorite order is storage order

- **WHEN** several projects are favorites
- **THEN** their submenus appear in the order `GetAllProjects()` returns, not by recency

#### Scenario: Favorite is conveyed by the icon, not the label

- **WHEN** a favorite project submenu is shown
- **THEN** its label is the project name only, with no ` *` suffix, and its icon is the favorite-star
  glyph

#### Scenario: No favorites

- **WHEN** no project is a favorite
- **THEN** a single disabled `(no favorite projects)` item appears where the project list would be

### Requirement: Initialized agents per project submenu

A project's submenu SHALL list the agents initialized for that project: a plugin that exposes a launch
command **and** is installed in the system **and** is available in that project. This initialized-agent
rule applies in addition to the configured default agent, which the separate "Default agent is first in
every project submenu" requirement governs: the exclusion scenarios below do not apply to the configured
default agent.

#### Scenario: Agent initialized in the project is listed

- **WHEN** a plugin exposes a launch command, is installed in the system, and is available in the project
- **THEN** it appears as an item in that project's submenu

#### Scenario: Agent not installed in the system is excluded

- **WHEN** a plugin is not installed in the system and it is not the configured default agent
- **THEN** no item for it appears in any project submenu

#### Scenario: Agent not available in the project is excluded

- **WHEN** a plugin is installed in the system but is not available in the project, and it is not the
  configured default agent
- **THEN** no item for it appears in that project's submenu

#### Scenario: Plugin without a launch command is excluded

- **WHEN** a plugin does not expose a launch command and it is not the configured default agent
- **THEN** no item for it appears in any project submenu

#### Scenario: Agent item shows the agent name

- **WHEN** an initialized agent appears in a project submenu
- **THEN** its label is the agent name only, with no `Run ` prefix, and invoking it executes the launch
  command for that agent in that project

#### Scenario: Empty project submenu

- **WHEN** a project has no default agent and no initialized agents
- **THEN** its submenu shows one disabled `(no agents)` item

### Requirement: Default agent is first in every project submenu

When `AppPreferences.DefaultAgentId` is set and resolves to a plugin in the catalog, that agent SHALL be
the first item of every project submenu, even when it is not initialized for the project. A separator
SHALL follow the default item only when the submenu also lists at least one other agent after
de-duplicating the default; when the default is the only item, the submenu SHALL contain exactly that
item, with no separator and no `(no agents)` placeholder. When no default is configured, or the
configured id is not in the catalog, the submenu SHALL NOT contain a default item or its separator.

#### Scenario: Default agent is first

- **WHEN** a default agent id is configured and resolves to a catalog plugin
- **THEN** that agent is the first item of each project submenu, labelled with the agent name only,
  followed by a separator when the submenu lists at least one other agent after it

#### Scenario: Default agent shown even when not initialized

- **WHEN** the default agent is not installed in the system, not available in the project, or does not
  expose a launch command
- **THEN** it still appears first in each project submenu

#### Scenario: Default agent that cannot launch is not runnable

- **WHEN** the default agent plugin does not expose a launch command
- **THEN** its first item is disabled

#### Scenario: An initialized default agent appears exactly once

- **WHEN** the default agent is also initialized for the project
- **THEN** it appears once, as the first item, followed by the separator when the submenu lists at
  least one other agent after it, and not again among the other agents

#### Scenario: Separator omitted when the default is the only item

- **WHEN** a default agent id is configured and resolves to a catalog plugin, and the project submenu
  lists no other agent after de-duplicating the default
- **THEN** the submenu contains exactly the default item, with no separator and no `(no agents)`
  placeholder

#### Scenario: No default configured

- **WHEN** `DefaultAgentId` is null or empty
- **THEN** no default item and no separator appear in any project submenu

#### Scenario: Default id not in the catalog

- **WHEN** `DefaultAgentId` is set but no catalog plugin has that id
- **THEN** no default item and no separator appear in any project submenu

### Requirement: Actionable menu items carry MDL2 icons

Every item that invokes a command SHALL show a `Segoe MDL2 Assets` glyph in its menu icon slot:
`FavoriteStar` on a project submenu, `Play` on an agent item including the default agent, `FolderAdd` on
`Add Project…`, `Settings` on `Settings`, `Sync` on `Updates`, `Sync` or `Update` on the dynamic update
item according to its state, `Page` on `What's New`, and `Exit` on `Exit`. The header, the disabled
placeholders, and separators SHALL NOT show an icon.

#### Scenario: Project submenu uses the favorite star

- **WHEN** a favorite project submenu is shown
- **THEN** its icon is the `FavoriteStar` glyph

#### Scenario: Agent items use the play glyph

- **WHEN** an agent item is shown in a project submenu, including a default agent item
- **THEN** its icon is the `Play` glyph

#### Scenario: Tail items use their glyphs

- **WHEN** the menu tail is shown
- **THEN** `Add Project…` uses `FolderAdd`, `Settings` uses `Settings`, `Updates` uses `Sync`, and
  `Exit` uses `Exit`

#### Scenario: Check item uses the sync glyph

- **WHEN** the dynamic update item offers or runs a check
- **THEN** its icon is the `Sync` glyph

#### Scenario: Download item uses the update glyph

- **WHEN** the dynamic update item reports a download or offers a download and restart
- **THEN** its icon is the `Update` glyph

#### Scenario: What's New uses the page glyph

- **WHEN** `What's New` is shown inside the `Updates` submenu
- **THEN** its icon is the `Page` glyph

#### Scenario: Header and placeholders carry no icon

- **WHEN** the header, a `(no favorite projects)` item, or a `(no agents)` item is shown
- **THEN** it has no icon

### Requirement: Updates submenu groups update surfaces

The menu SHALL show an `Updates` submenu whose items are, in order, the dynamic update item and
`What's New`. The dynamic item SHALL be `Check for updates` while idle, `Checking for updates…` while a
check runs, `Downloading update…` while a download runs, and `Download <version> and restart` when an
update is available. `Check for updates` SHALL remain reachable from the tray menu through this submenu.

#### Scenario: Updates submenu order

- **WHEN** the menu is shown
- **THEN** the `Updates` submenu contains the dynamic update item first and `What's New` second

#### Scenario: Check for updates is reachable from the tray

- **WHEN** no check or download is running and no update is known
- **THEN** the first `Updates` item is a clickable `Check for updates` that starts the shared update
  check

#### Scenario: Checking shows the progress item

- **WHEN** an update check is running
- **THEN** the first `Updates` item is a disabled `Checking for updates…`

#### Scenario: Downloading shows the progress item

- **WHEN** an update download is running
- **THEN** the first `Updates` item is a disabled `Downloading update…`

#### Scenario: Known update offers download and restart

- **WHEN** an update version is known and no download is running
- **THEN** the first `Updates` item is `Download <version> and restart` and invoking it requests the
  update download

#### Scenario: What's New remains available

- **WHEN** `What's New` is invoked from the `Updates` submenu
- **THEN** the release notes open as before

### Requirement: Launching from a project submenu selects the project

Invoking an agent item in project P's submenu SHALL make P the current project and then execute the agent
launch workflow with P.

#### Scenario: Launch selects the project first

- **WHEN** an agent item in project P's submenu is invoked and P is not the current project
- **THEN** P becomes the current project and the launch runs with P as its project context

#### Scenario: Launch runs the shared workflow with the project

- **WHEN** an agent item is invoked
- **THEN** the shared agent command workflow runs the launch command for that plugin in that project

### Requirement: Add Project is placed below the project list

`Add Project…` SHALL appear directly below the project submenus and before `Settings`.

#### Scenario: Add Project position

- **WHEN** the menu is shown
- **THEN** `Add Project…` appears after the last project submenu (or the `(no favorite projects)` item)
  and before `Settings`

#### Scenario: Add Project behavior is unchanged

- **WHEN** `Add Project…` is invoked
- **THEN** the project-folder workflow runs as before and the new project becomes current

### Requirement: Menu content refreshes when the menu opens

The tray menu SHALL rebuild its content from current state when it is opened, so newly favorited
projects, a changed default agent, and fresh detection results are reflected without restarting the
application.

#### Scenario: New favorite appears on next open

- **WHEN** a project is favorited while the application runs
- **THEN** it appears as a submenu the next time the menu opens

#### Scenario: Changed default agent appears on next open

- **WHEN** the configured default agent changes while the application runs
- **THEN** the new default appears first in each project submenu the next time the menu opens

#### Scenario: Fresh detection appears on next open

- **WHEN** an agent's installation or project availability changes
- **THEN** the next time the menu opens the initialized-agent lists reflect the new detection result

### Requirement: Tray menu visual theme

The tray context menu SHALL render with the application's themed context-menu styles in both
Windows apps light and dark modes. The visual change SHALL NOT alter the menu's structure, labels,
glyphs, separator rules, commands, or refresh-on-open behavior specified by the other requirements
of this capability.

#### Scenario: Themed rendering in Windows dark mode

- **WHEN** the tray menu is shown while Windows apps use the dark theme
- **THEN** it renders with the dark themed menu styles, and the project and Updates submenus render
  with the same styling

#### Scenario: Themed rendering in Windows light mode

- **WHEN** the tray menu is shown while Windows apps use the light theme
- **THEN** it renders with the light themed menu styles, and the project and Updates submenus render
  with the same styling

#### Scenario: Visual change only

- **WHEN** the tray menu is rendered with the themed styles
- **THEN** the item order, headers, icons, separator placement, enabled/disabled states, and command
  behavior remain exactly as specified by the other requirements of this capability

#### Scenario: Themed menu details

- **WHEN** the tray menu is rendered with the themed styles in either Windows mode
- **THEN** it shows a rounded card surface with a one-pixel border and a subtle shadow, hover and
  pressed highlights on entries, dimmed disabled entries, a visible submenu arrow, an icon slot
  whose glyphs follow the entry's text color, and themed separators
