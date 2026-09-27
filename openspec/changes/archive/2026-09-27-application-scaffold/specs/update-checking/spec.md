## Purpose

Checks for application updates via Velopack, notifies users of available updates, and provides version information for transparency and maintainability.

## ADDED Requirements

### Requirement: Automatic update check on startup

The system SHALL check for updates asynchronously when the application starts.

#### Scenario: Update check at startup
- **WHEN** the application starts
- **THEN** an update check runs in the background without blocking UI

#### Scenario: No update available
- **WHEN** update check completes and no update is available
- **THEN** nothing is displayed to the user and operation is logged

### Requirement: Update availability notification

The system SHALL notify the user when an update is available.

#### Scenario: Update available
- **WHEN** an update check finds a new version
- **THEN** the user is notified via system notification or tray icon

#### Scenario: Notification includes version
- **WHEN** an update notification is shown
- **THEN** it includes the new version number

### Requirement: Current version display

The system SHALL provide the current application version to users.

#### Scenario: Version in Settings
- **WHEN** the user opens Settings Window
- **THEN** the current version is displayed

#### Scenario: Version in About dialog
- **WHEN** the user views application information
- **THEN** the version number is visible

### Requirement: Update check failure handling

The system SHALL handle update check failures gracefully.

#### Scenario: Network error during check
- **WHEN** update check fails due to network issues
- **THEN** the failure is logged but does not block application use

#### Scenario: Update check timeout
- **WHEN** update check does not complete within timeout
- **THEN** the check is cancelled and operation continues

### Requirement: Manual update initiation

The system SHALL allow users to manually trigger update checks.

#### Scenario: Check for updates from Settings
- **WHEN** the user clicks "Check for Updates" in Settings
- **THEN** an immediate update check is performed

#### Scenario: Update download progress
- **WHEN** an update is being downloaded
- **THEN** progress is displayed to the user
