## ADDED Requirements

### Requirement: Agent list refresh preserves stable row identity

The launch-window agent list SHALL synchronize by stable plugin ID, preserving existing row identity for agents that remain in the target list.

#### Scenario: Existing agent remains available
- **WHEN** a refresh produces an agent with the same plugin ID as an existing row
- **THEN** the existing row is updated in place and is not replaced by a new row object

#### Scenario: New agent appears
- **WHEN** a refresh produces a plugin ID absent from the current list
- **THEN** one new row is added for that agent

#### Scenario: Agent disappears
- **WHEN** a refresh no longer produces a plugin ID currently shown
- **THEN** that row is removed and no stale row remains

### Requirement: Agent selection survives refresh

The launch-window agent selection SHALL remain attached to the same plugin ID when that agent remains in the refreshed list.

#### Scenario: Selected agent remains
- **WHEN** the selected agent is present after refresh
- **THEN** the refreshed collection selects the row with the same plugin ID

#### Scenario: Selected agent disappears
- **WHEN** the selected agent is absent after refresh
- **THEN** the selection is cleared or moved according to the existing empty-selection behavior

### Requirement: Agent row properties update in place

The launch-window agent row SHALL update availability, status, logo, and version-related properties without replacing the row when the plugin ID remains present.

#### Scenario: Availability changes
- **WHEN** project selection or filtering changes an agent's availability
- **THEN** the existing row reflects the new state through property notifications
