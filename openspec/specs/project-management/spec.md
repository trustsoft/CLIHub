# project-management Specification

## Purpose

Tracks the set of project directories the user works in, maintains the currently selected project as launch context, and resolves project identity (name and logo) so agents can be launched in the right folder.

## Requirements

### Requirement: Project registration

The system SHALL allow users to register a project by selecting a folder.

#### Scenario: Add a new project
- **WHEN** a user selects a folder that is not already registered
- **THEN** a project is created with a unique ID, the folder path, and a name derived from the folder name

#### Scenario: Reject duplicate folder
- **WHEN** a user selects a folder that is already registered
- **THEN** no duplicate project is created and the existing project is selected

#### Scenario: Reject non-existent folder
- **WHEN** a user selects a folder path that does not exist
- **THEN** registration fails with an error and no project is added

### Requirement: Project persistence

The system SHALL persist registered projects to the application configuration file.

#### Scenario: Projects survive restart
- **WHEN** projects have been registered and the application restarts
- **THEN** all registered projects are loaded from `%APPDATA%\CLIHub\config.json`

#### Scenario: Persist on change
- **WHEN** a project is added, removed, favorited, or selected
- **THEN** the change is written to the configuration file

### Requirement: Current project tracking

The system SHALL maintain a currently selected project that provides launch context.

#### Scenario: Set current project
- **WHEN** a user selects a project
- **THEN** that project becomes current and is saved as `currentProjectId`

#### Scenario: Restore current project on startup
- **WHEN** the application starts and `currentProjectId` references a registered project
- **THEN** that project is restored as the current project

#### Scenario: Clear current project
- **WHEN** the current project is removed
- **THEN** no project is current until another is selected

### Requirement: Recent projects ordering

The system SHALL track when each project was last used and list projects ordered by recency.

#### Scenario: Update last-used timestamp
- **WHEN** a project is selected or an agent is launched in it
- **THEN** the project's last-used timestamp is updated

#### Scenario: List recent projects
- **WHEN** recent projects are requested with a limit
- **THEN** projects are returned sorted by last-used descending, up to the limit

### Requirement: Favorite projects

The system SHALL allow marking projects as favorites, including from the launch window.

#### Scenario: Mark favorite
- **WHEN** a user marks a project as a favorite
- **THEN** the project's favorite flag is set and persisted

#### Scenario: Unmark favorite
- **WHEN** a user unmarks a favorite project
- **THEN** the project's favorite flag is cleared and persisted

#### Scenario: Toggle from the launch window
- **WHEN** the user invokes the favorite action for the selected project in the launch window
- **THEN** the project's favorite state is toggled, persisted, and reflected in the project list

#### Scenario: Toggle requires a selection
- **WHEN** no project is selected in the launch window
- **THEN** the favorite action is unavailable and no project's favorite state changes

### Requirement: Project logo detection

The system SHALL resolve a project logo from well-known image filenames in the project folder, falling back to a default. Resolution SHALL go through the persistent logo cache keyed by the project ID, so repeated lookups and restarts reuse resolved values without rescanning.

#### Scenario: Logo file found
- **WHEN** a project folder contains an image matching a known filename (for example `logo.png`, `icon.png`)
- **THEN** the first matching file is used as the project logo and stored in the cache under the project's ID

#### Scenario: No logo file found
- **WHEN** a project folder contains no known logo filename
- **THEN** the default project logo is used and the negative outcome is cached under the project's ID

#### Scenario: Cached resolution is reused
- **WHEN** a project logo is requested again for a project whose resolution is already cached
- **THEN** the cached value is returned without scanning the project folder again

#### Scenario: Resolution persists across restarts
- **WHEN** the application restarts
- **THEN** previously resolved project logos are reused from the cache state file without rescanning

#### Scenario: Manual refresh re-resolves
- **WHEN** the user triggers a refresh in the launch window
- **THEN** cached project logo entries are cleared and project logos are re-resolved from the project folders

### Requirement: Project removal

The system SHALL allow removing a registered project without deleting its files.

#### Scenario: Remove project
- **WHEN** a user removes a project
- **THEN** it is removed from configuration and no longer appears in lists

#### Scenario: Files are not deleted
- **WHEN** a project is removed
- **THEN** the project folder and its contents on disk are left untouched

#### Scenario: Remove from the launch window
- **WHEN** a user selects a project in the launch window and activates "Remove Project"
- **THEN** a confirmation prompt is shown, and the project is removed from the list only after the user confirms

#### Scenario: Refresh after removal
- **WHEN** a project is removed from the launch window
- **THEN** the project list and the agent availability display are refreshed to reflect the removal

### Requirement: Launch context

The system SHALL use the current project's folder as the working directory when launching an agent.

#### Scenario: Launch in current project
- **WHEN** an agent is launched while a project is current
- **THEN** the terminal session starts in the current project's folder

#### Scenario: Launch without a current project
- **WHEN** an agent is launched and no project is current
- **THEN** the launch is blocked with a message asking the user to select a project
