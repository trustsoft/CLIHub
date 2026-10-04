# update-checking Specification

## Purpose

Checks for, downloads, and applies application updates via Velopack, notifies users of available updates and download outcomes, and provides version information for transparency and maintainability.

## Requirements

### Requirement: Automatic update check on startup

The system SHALL check for updates asynchronously when the application starts, unless the startup check is disabled in preferences.

#### Scenario: Update check at startup
- **WHEN** the application starts and the startup check is enabled
- **THEN** an update check runs in the background without blocking the UI

#### Scenario: Startup check disabled
- **WHEN** the application starts and the startup check is disabled in preferences
- **THEN** no automatic update check runs

#### Scenario: No update available
- **WHEN** the update check completes and no update is available
- **THEN** nothing is shown to the user and the outcome is logged

#### Scenario: Not installed by the updater
- **WHEN** the application was not installed by Velopack (for example, run from a build output)
- **THEN** the check is skipped, the outcome is logged, and the application continues normally

### Requirement: Update availability notification

The system SHALL notify the user when an update is available.

#### Scenario: Update available
- **WHEN** the update check finds a newer version
- **THEN** a system notification is shown

#### Scenario: Notification includes version
- **WHEN** an update notification is shown
- **THEN** it includes the available version number

### Requirement: Current version display

The system SHALL show the current application version to the user.

#### Scenario: Version visible in the window
- **WHEN** the user opens the CLIHub window
- **THEN** the current application version is displayed

### Requirement: Manual update check

The system SHALL allow the user to trigger an update check manually.

#### Scenario: Manual check
- **WHEN** the user invokes "Check for updates"
- **THEN** an update check runs immediately and its outcome is reported to the user

### Requirement: Update check failure handling

The system SHALL handle update-check failures gracefully.

#### Scenario: Network error during check
- **WHEN** the check fails due to a network or service error
- **THEN** the failure is logged and reported as a failed check without blocking the application

#### Scenario: Update check timeout
- **WHEN** the check does not complete within its timeout
- **THEN** it is cancelled and the application continues normally

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

### Requirement: Launch window update control

The launch window footer SHALL provide a single update control that displays the current application version when idle, triggers an update check when activated in the idle state, and acts as the download-and-restart action when an update is available. The control SHALL share the update state with the tray menu and the What's New window.

#### Scenario: Idle control shows the version and triggers a check
- **WHEN** the launch window is shown and no update is known
- **THEN** the update control displays the current application version, and activating it runs an update check whose outcome is reported in the footer status line

#### Scenario: Check in progress
- **WHEN** an update check triggered from the control is running
- **THEN** the control shows a checking state and does not start a second check

#### Scenario: Update found without user action
- **WHEN** a check finds a newer version, including the automatic startup check
- **THEN** the control reads "Update to \<version\>" with an accent visual treatment, without the user invoking anything

#### Scenario: Update available starts the download
- **WHEN** the control in the update-available state is activated
- **THEN** the update download starts and the control switches to the downloading state

#### Scenario: Downloading state
- **WHEN** an update download is running
- **THEN** the control reads "Downloading \<version\>…", does not start a second download, and reports the outcome when it finishes

#### Scenario: Shared download state
- **WHEN** a download started from the tray menu or the What's New window is running while the launch window is shown
- **THEN** the control shows the same downloading state and does not start a second download

#### Scenario: Restart offered after download
- **WHEN** the download completes successfully
- **THEN** the control reads "Restart to update to \<version\>" with an accent visual treatment, and activating it applies the update and restarts the application into the new version

#### Scenario: Download failure returns to the action state
- **WHEN** the update download fails
- **THEN** the control returns to the update-available state, and the failure is reported in the footer status line while the application keeps running the current version

#### Scenario: No update actions for non-managed installs
- **WHEN** the application was not installed by the updater and the update control is used
- **THEN** the control keeps showing the current version and the check behavior, offers no update or restart actions, and the outcome is reported in the footer status line
