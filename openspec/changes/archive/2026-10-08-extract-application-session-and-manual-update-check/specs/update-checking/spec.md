## MODIFIED Requirements

### Requirement: Manual update check

The system SHALL allow the user to trigger an update check manually from the launch window, tray menu, and What's New window. Manual checks SHALL use the shared update state, and each surface SHALL prevent repeat invocation through that same surface while its check is in progress.

#### Scenario: Manual check
- **WHEN** the user invokes "Check for updates" from an update entry point
- **THEN** an update check runs immediately and its outcome is reported or reflected through the existing update state

#### Scenario: Manual check from the launch window
- **WHEN** the user invokes "Check for updates" from the launch-window update control
- **THEN** an update check runs immediately and its outcome is reported in the footer status line

#### Scenario: Manual check from the tray
- **WHEN** no update is known and no update check or download is running
- **THEN** the tray menu offers "Check for updates"
- **AND WHEN** the user invokes the action
- **THEN** a manual check starts and the tray reflects its checking state until it completes

#### Scenario: Manual check from What's New
- **WHEN** no update is known and no update check or download is running while What's New is open
- **THEN** the window offers "Check for updates"
- **AND WHEN** the user invokes the action
- **THEN** a manual check starts and the window reflects its checking state until it completes

#### Scenario: Shared availability after a manual check
- **WHEN** a manual check finds an available update
- **THEN** the shared update state changes and the tray and What's New surfaces offer their existing download-and-restart action

#### Scenario: No update or failed check
- **WHEN** a manual check completes without an available update, fails, is cancelled, or reports that the application is not updater-installed
- **THEN** the checking state ends and the manual check action becomes available again when no download is running

#### Scenario: Repeated check invocation is ignored
- **WHEN** a manual update check started from the tray or What's New is already running, or an update download is active
- **THEN** that same surface does not start another manual check
