# project-registry Specification

## Purpose

Maintains the list of projects the user launches agents in, so a project folder is
picked once and reused, and persists that list through the configuration store.

## Requirements

### Requirement: Add a project from a folder

The system SHALL add a project from a selected folder. The project's default name
SHALL be the folder name, and the new project SHALL be persisted to
`projects.json`.

#### Scenario: Add a valid folder

- **WHEN** the user adds an existing folder that is not already registered
- **THEN** a project is created, named after the folder, and written to `projects.json`

#### Scenario: Default name from folder

- **WHEN** a project is added for the folder `C:\work\demo`
- **THEN** its name is `demo`

### Requirement: Validate project entries

The system SHALL validate a project before adding it: the path SHALL exist and be a
directory, the path SHALL NOT duplicate an existing project, and the name SHALL be
non-empty.

#### Scenario: Path does not exist

- **WHEN** the added path does not exist or is not a directory
- **THEN** the project is rejected and no entry is written

#### Scenario: Duplicate path

- **WHEN** the added path is already registered
- **THEN** the project is rejected and the existing entry is unchanged

### Requirement: Project identity

Each project SHALL have an identifier that is unique within the registry.

#### Scenario: Unique identifier

- **WHEN** a project is added
- **THEN** its identifier differs from every existing project's identifier

### Requirement: Remove a project

The system SHALL remove a project from the registry and persist the change
to `projects.json` without touching files on disk.

#### Scenario: Remove an entry

- **WHEN** the user removes a project
- **THEN** the entry disappears from the registry and `projects.json`, and the folder on disk is left intact
