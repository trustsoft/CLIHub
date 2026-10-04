# update-checking Delta

## ADDED Requirements

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
