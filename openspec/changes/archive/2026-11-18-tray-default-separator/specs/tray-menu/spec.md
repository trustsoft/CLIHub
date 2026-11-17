## MODIFIED Requirements

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
