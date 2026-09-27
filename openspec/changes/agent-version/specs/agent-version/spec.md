## Purpose

Retrieves each agent's version via its own version command and presents it in the agent list without blocking the UI or requiring a selected project.

## ADDED Requirements

### Requirement: Version lookup

The system SHALL retrieve an agent's version by running its declared `version` command and capturing the output.

#### Scenario: Version command defined
- **WHEN** an agent declares a `version` command
- **THEN** the command is executed and its trimmed standard output is returned as the version

#### Scenario: No version command
- **WHEN** an agent does not declare a `version` command
- **THEN** no process is started and the version is reported as unknown

#### Scenario: Version command fails
- **WHEN** the version command cannot start or exits non-zero
- **THEN** the version is reported as unknown and the failure is logged

### Requirement: Project-independent lookup

The system SHALL retrieve versions without requiring a selected project.

#### Scenario: No current project
- **WHEN** no project is selected and versions are requested
- **THEN** versions are still retrieved

### Requirement: Version caching

The system SHALL cache retrieved versions per agent until invalidated.

#### Scenario: Repeated lookup uses cache
- **WHEN** a version has already been retrieved for an agent
- **THEN** a subsequent lookup returns the cached value without running the command again

#### Scenario: Cache invalidation
- **WHEN** the cache is invalidated
- **THEN** the next lookup runs the version command again

### Requirement: Non-blocking presentation

The system SHALL render the agent list before versions resolve and update it as versions become available.

#### Scenario: List appears immediately
- **WHEN** the agent list is shown
- **THEN** agents are displayed before their versions have been retrieved

#### Scenario: Versions fill in
- **WHEN** version retrieval completes for an agent
- **THEN** the displayed version for that agent is updated

### Requirement: Unknown version display

The system SHALL display a clear placeholder when a version is not available.

#### Scenario: Unknown shown
- **WHEN** an agent's version is unknown
- **THEN** the list shows a placeholder (for example, `unknown`) for that agent
