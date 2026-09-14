## MODIFIED Requirements

### Requirement: Add a project from a folder

The system SHALL add a project from a selected folder. The project's default
name SHALL be the folder name, and the new project SHALL be persisted to
`projects.json`.

#### Scenario: Add a valid folder

- **WHEN** the user adds an existing folder that is not already registered
- **THEN** a project is created, named after the folder, and written to `projects.json`

#### Scenario: Default name from folder

- **WHEN** a project is added for the folder `C:\work\demo`
- **THEN** its name is `demo`

### Requirement: Remove a project

The system SHALL remove a project from the registry and persist the change
to `projects.json` without touching files on disk.

#### Scenario: Remove an entry

- **WHEN** the user removes a project
- **THEN** the entry disappears from the registry and `projects.json`, and the folder on disk is left intact
