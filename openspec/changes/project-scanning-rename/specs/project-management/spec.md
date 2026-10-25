# project-management delta

## MODIFIED Requirements

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
