## ADDED Requirements

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

### Requirement: Filter preference persists

The system SHALL persist the filter preference across sessions.

#### Scenario: Preference restored
- **WHEN** the application restarts
- **THEN** the filter reflects the previously saved preference

#### Scenario: Default is off
- **WHEN** no preference has been saved
- **THEN** the filter defaults to disabled (dimming behavior)
