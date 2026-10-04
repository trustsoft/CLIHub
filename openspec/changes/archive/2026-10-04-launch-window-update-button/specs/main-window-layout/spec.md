# main-window-layout Delta

## MODIFIED Requirements

### Requirement: Window footer

The system SHALL provide a footer that shows the application identity, keeps the update control - which itself displays the current application version - in the left part of the footer immediately to the right of the application name, and exposes the window-level actions.

#### Scenario: Footer identity and version
- **WHEN** the main window is shown
- **THEN** the left part of the footer shows the application name followed by the update control, and the control displays the current application version

#### Scenario: Update check from the footer
- **WHEN** the user activates the footer's update control while it is idle
- **THEN** an update check runs immediately and its outcome is reported in the footer

#### Scenario: Footer actions
- **WHEN** the main window is shown
- **THEN** the footer exposes actions to add a project, open the application data folder, open Settings, and exit the application

#### Scenario: Open the data folder from the footer
- **WHEN** the user activates the footer's open-data-folder action
- **THEN** the application data folder is opened in the system file browser

#### Scenario: Add a project from the footer
- **WHEN** the user activates the footer's add-project action
- **THEN** a folder picker opens and the selected folder is registered and made the current project

#### Scenario: Open Settings from the footer
- **WHEN** the user activates the footer's settings action
- **THEN** the Settings window opens, or the existing Settings window is brought to the front

#### Scenario: Exit from the footer
- **WHEN** the user activates the footer's exit action
- **THEN** the application exits and its tray icon is removed

#### Scenario: Transient status messages
- **WHEN** an action reports status or a failure
- **THEN** the message is shown in the footer, is replaced by the next message, and leaves the footer's actions in place
