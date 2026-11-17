## ADDED Requirements

### Requirement: Tray menu structure

The tray context menu SHALL show, top to bottom: a disabled current-project header; one submenu per
favorite project; `Add Project…`; `Settings`; `What's New`; the update item; a separator; `Show CLIHub`;
and `Exit`.

#### Scenario: Header reflects the current project

- **WHEN** a project is current
- **THEN** the first item is a disabled `Current: <name>` header

#### Scenario: Header when no project is selected

- **WHEN** no project is current
- **THEN** the first item is a disabled `No project selected` header

#### Scenario: Settings, What's New, update, Show CLIHub, and Exit keep their behavior

- **WHEN** the menu is shown
- **THEN** `Settings`, `What's New`, the update item, the separator, `Show CLIHub`, and `Exit` appear
  below `Add Project…` in that order with unchanged behavior

#### Scenario: Recent Projects and Launch Agent are gone

- **WHEN** the menu is shown
- **THEN** no `Recent Projects` submenu and no `Launch Agent` submenu are present

### Requirement: Favorite project submenus

The tray context menu SHALL show one submenu per project whose `IsFavorite` is true, in the storage order
returned by `GetAllProjects()`, and SHALL show no project submenu for a project that is not a favorite.

#### Scenario: Only favorites are shown

- **WHEN** the registered projects include favorites and non-favorites
- **THEN** exactly the favorites appear as top-level submenus

#### Scenario: Favorite order is storage order

- **WHEN** several projects are favorites
- **THEN** their submenus appear in the order `GetAllProjects()` returns, not by recency

#### Scenario: Favorite marker in the header

- **WHEN** a favorite project submenu is shown
- **THEN** its label carries the existing favorite marker convention (`<Name> *`)

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

#### Scenario: Agent item is a Run action

- **WHEN** an initialized agent appears in a project submenu
- **THEN** its label expresses a run action (`Run <AgentName>`) and invoking it executes the launch
  command

#### Scenario: Empty project submenu

- **WHEN** a project has no default agent and no initialized agents
- **THEN** its submenu shows one disabled `(no agents)` item

### Requirement: Default agent is first in every project submenu

When `AppPreferences.DefaultAgentId` is set and resolves to a plugin in the catalog, that agent SHALL be
the first item of every project submenu, followed by a separator, even when it is not initialized for the
project. When no default is configured, or the configured id is not in the catalog, the submenu SHALL NOT
contain a default item or its separator.

#### Scenario: Default agent is first

- **WHEN** a default agent id is configured and resolves to a catalog plugin
- **THEN** that agent is the first item of each project submenu, followed by a separator

#### Scenario: Default agent shown even when not initialized

- **WHEN** the default agent is not installed in the system, not available in the project, or does not
  expose a launch command
- **THEN** it still appears first in each project submenu

#### Scenario: Default agent that cannot launch is not runnable

- **WHEN** the default agent plugin does not expose a launch command
- **THEN** its first item is disabled

#### Scenario: An initialized default agent appears exactly once

- **WHEN** the default agent is also initialized for the project
- **THEN** it appears once, as the first item, followed by the separator, and not again among the other
  agents

#### Scenario: No default configured

- **WHEN** `DefaultAgentId` is null or empty
- **THEN** no default item and no separator appear in any project submenu

#### Scenario: Default id not in the catalog

- **WHEN** `DefaultAgentId` is set but no catalog plugin has that id
- **THEN** no default item and no separator appear in any project submenu

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
