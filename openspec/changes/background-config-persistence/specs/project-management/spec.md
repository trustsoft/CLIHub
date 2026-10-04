## MODIFIED Requirements

### Requirement: Project persistence

The system SHALL persist registered projects to the application configuration file.

#### Scenario: Projects survive restart
- **WHEN** projects have been registered and the application restarts
- **THEN** all registered projects are loaded from `%APPDATA%\CLIHub\config.json`

#### Scenario: Persist on change
- **WHEN** a project is added, removed, favorited, or selected
- **THEN** the change is queued for prompt asynchronous persistence and reaches the configuration file without blocking the interface
