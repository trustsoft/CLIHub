# project-management (partial): manual project path correction

## MODIFIED Requirements

### Requirement: Settings project registry page

The Settings window SHALL provide a Projects page that lists every registered project with its name, full folder path, provenance, and whether its folder currently exists. The page SHALL allow adding a manual project by entering or selecting an existing folder path and registering it, removing a manual project after confirmation, **and changing a manual project's folder path while preserving its identity**. Adding from the page SHALL NOT change the current project. Discovered projects SHALL be listed read-only. Project changes made on the page SHALL apply immediately, independently of the Settings footer Save and Cancel, and every other surface SHALL reflect them without a restart.

#### Scenario: Registry lists every project

- **WHEN** the Projects page opens
- **THEN** every registered project is listed with its name, full folder path, and provenance
- **AND** a project whose folder no longer exists is marked as missing its folder

#### Scenario: Manual project offers management actions

- **WHEN** a manually registered project is listed
- **THEN** the row offers a remove action
- **AND** the row offers a change-folder action
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

- **WHEN** the user adds, removes, or changes the folder of a project and then closes the Settings window with Cancel
- **THEN** the project change remains applied

#### Scenario: Other surfaces follow the registry

- **WHEN** a project is added, removed, or has its folder changed on the Projects page
- **THEN** the Launch Window project list and the tray reflect the change without restarting the application

#### Scenario: Change a manual project's folder path

- **WHEN** the user activates the change-folder action on a manual project row, selects an existing folder that is not registered to a different project, and confirms
- **THEN** the project's folder path is updated to the selected folder
- **AND** the project's name is re-derived from the selected folder's name
- **AND** the project's identity, provenance, favorite flag, and last-used timestamp are preserved
- **AND** the project's logo cache entry is invalidated and the row re-enriches immediately
- **AND** the change is persisted immediately
- **AND** the project folder's contents on disk are left untouched

#### Scenario: Change folder to the same path

- **WHEN** the user selects the project's current folder path (normalized)
- **THEN** the project remains unchanged and no error is reported

#### Scenario: Change folder to a non-existent path

- **WHEN** the user selects a folder that does not exist
- **THEN** the change is rejected with an error and the project remains unchanged

#### Scenario: Change folder to a path already registered to a different project

- **WHEN** the user selects a folder that is already registered to a different project
- **THEN** the change is rejected with an error and the project remains unchanged

#### Scenario: Change folder action unavailable for discovered projects

- **WHEN** a discovered project is listed
- **THEN** the change-folder action is not available on that row

#### Scenario: Change folder preserves current selection

- **WHEN** the current project's folder is changed
- **THEN** the project remains the current selection after the change

#### Scenario: Project name always derived from folder

- **WHEN** a project's folder is changed
- **THEN** the project's display name is derived from the new folder name
- **AND** no separate rename or manual name-editing action is required
