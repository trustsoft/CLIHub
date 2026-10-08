## ADDED Requirements

### Requirement: Shared application update workflow

The system SHALL route startup, tray, What's New, and launch-window update checks and operations through one application workflow that exposes the shared update state. Concurrent checks from any entry points SHALL join the active check rather than starting another check. The workflow SHALL preserve the existing entry-point policies for applying downloaded updates.

#### Scenario: Concurrent checks share one operation
- **WHEN** an update check is already running from any entry point and another entry point requests a check
- **THEN** the second request joins the active check and all surfaces observe the same checking and availability state

#### Scenario: Shared update availability
- **WHEN** the shared check finds an available update
- **THEN** the tray, What's New window, and launch-window update control observe the same available version

#### Scenario: Tray and What's New apply policy
- **WHEN** an update download is requested from the tray or What's New window
- **THEN** the shared workflow downloads the update, reports the outcome, and applies it with the existing automatic restart behavior

#### Scenario: Launch-window apply policy
- **WHEN** an update download is requested from the launch-window update control
- **THEN** the shared workflow downloads the update and the control offers its existing explicit restart action after a successful download

#### Scenario: Shared download exclusion
- **WHEN** an update download is already running and another entry point requests a download
- **THEN** the shared update service starts no second download and all surfaces continue to show the shared downloading state

## MODIFIED Requirements

### Requirement: Manual update check

The system SHALL allow the user to trigger an update check manually from the launch window, tray menu, and What's New window. Manual checks SHALL use the shared update state, and every entry point SHALL join an already-running check rather than starting a duplicate.

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
- **WHEN** a manual update check is already running from any entry point
- **THEN** another request joins that check and does not start a duplicate operation
