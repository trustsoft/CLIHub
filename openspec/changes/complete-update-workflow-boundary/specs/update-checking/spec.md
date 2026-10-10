## MODIFIED Requirements

### Requirement: Manual update check

The system SHALL allow the user to trigger an update check manually from the launch window, tray menu, What's New window, and Settings window. Manual checks SHALL use the shared update state, and every entry point SHALL join an already-running check rather than starting a duplicate.

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

#### Scenario: Manual check from Settings
- **WHEN** the user invokes "Check for updates" from Settings
- **THEN** the check participates in the shared update lifecycle and Settings reports the outcome in its status message

#### Scenario: Settings joins a check started elsewhere
- **WHEN** an update check is already running from another entry point and the user invokes the Settings check
- **THEN** Settings joins the active check without starting another underlying check and reports that check's outcome

#### Scenario: Shared availability after a manual check
- **WHEN** a manual check finds an update
- **THEN** the shared update state changes and all open update surfaces observe the same available version

#### Scenario: No update or failed check
- **WHEN** a manual check completes without an available update, fails, is cancelled, or reports that the application is not updater-installed
- **THEN** the checking state ends and the manual check action becomes available again when no download is running

#### Scenario: Repeated check invocation is ignored
- **WHEN** a manual update check is already running from any entry point
- **THEN** another request joins that check and does not start a duplicate operation
