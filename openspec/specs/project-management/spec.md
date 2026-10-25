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
- **THEN** the change is queued for prompt asynchronous persistence and reaches the configuration file without blocking the interface

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

The system SHALL resolve a project logo from well-known image filenames in the project folder, falling back to a default. Resolution SHALL go through the persistent logo cache keyed by the project ID, so repeated lookups and restarts reuse resolved values without rescanning. An agent-pane refresh SHALL NOT clear project logo cache entries.

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
- **THEN** only agent logo cache entries are cleared; project logo cache entries are preserved and the project folders are not rescanned for that refresh
- **AND** a project whose logo file changed on disk is picked up the next time its project logo entries are invalidated (project removal, or a full cache clear)

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

### Requirement: Project provenance

The system SHALL record whether each project was registered manually or discovered by scanning, and SHALL keep that provenance stable once set.

#### Scenario: Manually registered project is marked manual
- **WHEN** a user registers a project by selecting a folder
- **THEN** the project is marked as manually registered

#### Scenario: Discovered project is marked discovered
- **WHEN** a project is registered by repository scanning
- **THEN** the project is marked as discovered

#### Scenario: Manual project is never reclassified
- **WHEN** scanning later encounters a folder that was registered manually
- **THEN** the project's provenance remains manual

#### Scenario: Provenance defaults to manual
- **WHEN** a stored project has no provenance recorded
- **THEN** it is treated as manually registered

#### Scenario: Provenance persists
- **WHEN** a project's provenance is set and the application restarts
- **THEN** the stored provenance is restored

### Requirement: Discovered projects remain fully usable

The system SHALL treat discovered projects as ordinary projects for selection, recency, favorites, logo resolution, launch context, and removal.

#### Scenario: Discovered project is selectable
- **WHEN** a discovered project is selected
- **THEN** it becomes the current project and provides launch context like any other project

#### Scenario: Discovered project can be removed
- **WHEN** a user removes a discovered project
- **THEN** it is removed from configuration and its files on disk are left untouched

### Requirement: Settings project registry page

The Settings window SHALL provide a Projects page that lists every registered project with its name, full folder path, provenance, and whether its folder currently exists. The page SHALL allow adding a manual project by entering or selecting an existing folder path and registering it, and removing a manual project after confirmation. Adding from the page SHALL NOT change the current project. Discovered projects SHALL be listed read-only. Project changes made on the page SHALL apply immediately, independently of the Settings footer Save and Cancel, and every other surface SHALL reflect them without a restart.

#### Scenario: Registry lists every project

- **WHEN** the Projects page opens
- **THEN** every registered project is listed with its name, full folder path, and provenance
- **AND** a project whose folder no longer exists is marked as missing its folder

#### Scenario: Manual project offers management actions

- **WHEN** a manually registered project is listed
- **THEN** the row offers a remove action
- **AND** the page offers an add action

#### Scenario: Discovered project is read-only

- **WHEN** a discovered project is listed
- **THEN** the row offers no project mutation action
- **AND** the page states that Project Scanning manages discovered projects

#### Scenario: Open the project folder

- **WHEN** the user activates the open-folder action on a project row
- **THEN** the project folder opens in the operating system's file manager
- **AND** the action is unavailable when the folder no longer exists

#### Scenario: Add a manual project from Settings

- **WHEN** the user enters or selects an existing folder path that is not registered and registers it
- **THEN** the project is registered with manual provenance
- **AND** the project is persisted immediately
- **AND** the current project does not change

#### Scenario: Reject an empty or invalid path

- **WHEN** the user registers a blank path or a path that is not an existing absolute folder
- **THEN** registration reports an error and no project is added

#### Scenario: Add an already registered folder

- **WHEN** the user selects a folder that is already registered
- **THEN** no duplicate project is created and the existing project remains

#### Scenario: Add a non-existent folder

- **WHEN** the selected folder does not exist
- **THEN** registration fails with an error and no project is added

#### Scenario: Remove a manual project from Settings

- **WHEN** the user removes a manual project and confirms the removal
- **THEN** the project is removed from configuration and disappears from the list
- **AND** the project folder and its contents on disk are left untouched
- **AND** the removal is persisted immediately

#### Scenario: Cancel a removal

- **WHEN** the user cancels the removal confirmation
- **THEN** the project remains registered and the list is unchanged

#### Scenario: Settings footer does not govern project changes

- **WHEN** the user adds or removes a project and then closes the Settings window with Cancel
- **THEN** the project change remains applied

#### Scenario: Other surfaces follow the registry

- **WHEN** a project is added or removed on the Projects page
- **THEN** the Launch Window project list and the tray reflect the change without restarting the application

### Requirement: Enriched project display models

UI surfaces SHALL display project rows through enriched display models combining the project record's identity with an enriched snapshot: the final logo path (default-logo fallback already applied by the project logo resolver) and whether the project folder currently exists. The launch window and the Settings project registry SHALL share one display-model instance per project ID, and refresh SHALL update it in place rather than rebuild rows.

#### Scenario: Model carries the final logo path

- **WHEN** a project row is built or refreshed
- **THEN** its display model holds the resolved logo path with the default-logo fallback already applied
- **AND** the view renders it through the shared logo image converter

#### Scenario: Snapshot refresh triggers

- **WHEN** the project set changes, the launch window is shown or activated, or the Settings Projects page loads
- **THEN** the display models are re-enriched: folder existence is recomputed and the logo path is read through the project logo resolver and its persistent cache

#### Scenario: No live folder monitoring

- **WHEN** a project folder is created or deleted without any refresh trigger occurring
- **THEN** the displayed folder availability stays at its last snapshot until the next trigger

#### Scenario: One shared instance per project

- **WHEN** the same project is displayed by the launch window and the Settings registry
- **THEN** both surfaces display the same display-model instance
- **AND** a refresh updates that instance in place so both surfaces reflect the change without rebuilding their lists

### Requirement: Detached project reads

The project service SHALL return detached project copies from its read methods. UI surfaces SHALL bind project display models, not project records returned by the service, and mutating a returned project record SHALL NOT change registered project state.

#### Scenario: Reads return detached copies

- **WHEN** the project service returns projects from a read method
- **THEN** mutating a returned project changes neither later reads nor persisted state

#### Scenario: UI binds display models

- **WHEN** the launch window or the Settings registry renders project rows
- **THEN** the bound rows are enriched display models, not project records
