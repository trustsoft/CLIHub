## Purpose

Manages project directory tracking, current project selection, recent projects list, and project metadata persistence for context awareness.

## ADDED Requirements

### Requirement: Project registration

The system SHALL allow users to register project directories.

#### Scenario: Add new project
- **WHEN** a user adds a project directory
- **THEN** the project is assigned a unique ID and stored in configuration

#### Scenario: Project metadata capture
- **WHEN** a project is registered
- **THEN** its name, path, and creation timestamp are recorded

### Requirement: Current project tracking

The system SHALL maintain awareness of the currently selected project.

#### Scenario: Set current project
- **WHEN** a user selects a project
- **THEN** the project becomes the current context and is saved to config

#### Scenario: Get current project
- **WHEN** the application requests the current project
- **THEN** the project object for currentProjectId is returned

### Requirement: Recent projects list

The system SHALL track recently accessed projects ordered by last use.

#### Scenario: Update last used timestamp
- **WHEN** a project is selected or an agent is launched in it
- **THEN** the project's lastUsed timestamp is updated

#### Scenario: Recent projects retrieval
- **WHEN** recent projects are requested with a limit
- **THEN** projects are returned sorted by lastUsed descending, up to the limit

### Requirement: Project favorites

The system SHALL support marking projects as favorites.

#### Scenario: Toggle favorite status
- **WHEN** a user marks a project as favorite
- **THEN** the isFavorite flag is set and persisted

#### Scenario: List favorites
- **WHEN** favorite projects are requested
- **THEN** all projects with isFavorite=true are returned

### Requirement: Project removal

The system SHALL allow removing projects from the list.

#### Scenario: Remove project
- **WHEN** a user removes a project
- **THEN** it is deleted from config and no longer appears in lists

#### Scenario: Remove current project
- **WHEN** the current project is removed
- **THEN** currentProjectId is cleared and no project is selected
