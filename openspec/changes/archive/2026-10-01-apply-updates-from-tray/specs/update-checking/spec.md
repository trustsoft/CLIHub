# Spec Delta

## ADDED Requirements

### Requirement: Download and restart action

The system SHALL offer a single action that downloads the available update and then restarts the application into the new version. The action SHALL be available from the tray menu when an update has been found and the application is an updater-managed install.

#### Scenario: Action offered when an update is available
- **WHEN** an update check finds a newer version and the application is an updater-managed install
- **THEN** the tray menu contains an action to download the update and restart

#### Scenario: Action not offered without an update
- **WHEN** the last check found no update, failed, or the application is not an updater-managed install
- **THEN** the tray menu contains no download action

#### Scenario: One click completes the flow
- **WHEN** the user invokes the download-and-restart action
- **THEN** the update is downloaded and the application restarts into the new version without further prompts

### Requirement: Update download progress display

The system SHALL reflect the state of the update download in the tray menu while it runs.

#### Scenario: Downloading state is shown
- **WHEN** the update download is running
- **THEN** the tray menu item indicates that the update is downloading and does not offer to start another one

#### Scenario: Repeated invocation is ignored
- **WHEN** the download-and-restart action is invoked while a download is already running
- **THEN** no second download starts and the application continues with the running one

### Requirement: Restart notification

The system SHALL notify the user when the downloaded update is applied.

#### Scenario: Restart begins after download
- **WHEN** the update download completes successfully
- **THEN** a tray notification is shown and the application restarts

#### Scenario: New version is running after the update
- **WHEN** the application restarts after a completed update
- **THEN** the running version is the version that was previously reported as available

### Requirement: Update action in the What's New window

The What's New window SHALL offer the same download-and-restart action when an update is available.

#### Scenario: Action shown in the window
- **WHEN** the What's New window is open and an update is available
- **THEN** the window offers an action to download the update and restart

#### Scenario: Action hidden without an update
- **WHEN** the What's New window is open and no update is available
- **THEN** the window shows no download action

#### Scenario: Shared download state
- **WHEN** a download started from the tray menu is running while the What's New window is open
- **THEN** the window shows the same downloading state and does not start a second download

### Requirement: Update download failure handling

The system SHALL handle update download and apply failures gracefully.

#### Scenario: Download fails
- **WHEN** the update download fails due to a network or service error
- **THEN** the failure is logged, a tray notification reports it, the application keeps running the current version, and the action becomes available again

#### Scenario: Restart does not happen
- **WHEN** the restart into the new version does not occur after a completed download
- **THEN** the application continues running the current version, the situation is logged, and the action remains available

#### Scenario: No action for non-managed installs
- **WHEN** the application is not an updater-managed install and the update entry point is reached
- **THEN** no download or restart is attempted and the situation is logged
