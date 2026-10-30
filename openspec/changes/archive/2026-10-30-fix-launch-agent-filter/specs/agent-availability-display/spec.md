# agent-availability-display Specification Delta

## MODIFIED Requirements

### Requirement: Hide unavailable agents (optional filter)

The system SHALL support an optional mode that hides agents not available in the current project.

#### Scenario: Filter enabled with a project selected
- **WHEN** the filter is enabled and a project is selected
- **THEN** only agents available in that project are listed

#### Scenario: Filter disabled
- **WHEN** the filter is disabled and a project is selected
- **THEN** all agents are listed, with unavailable ones dimmed

#### Scenario: Filter enabled with no project selected
- **WHEN** the filter is enabled and no project is selected
- **THEN** all agents are listed

#### Scenario: Filter precedes dimming
- **WHEN** the filter is enabled
- **THEN** unavailable agents are hidden rather than dimmed

#### Scenario: Launch window toggle applies filter immediately
- **WHEN** the user toggles "Only agents available in project" in the launch window's Agents-menu
- **THEN** the agent list is refreshed with the new filter value
- **AND** the preference is persisted
- **AND** toggling the filter does not cause a property-change loop
