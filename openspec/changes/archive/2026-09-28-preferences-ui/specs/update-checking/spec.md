## MODIFIED Requirements

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
