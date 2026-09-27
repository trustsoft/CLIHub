# update-checking Specification

## Purpose

Checks for application updates via Velopack, notifies users of available updates, and provides version information for transparency and maintainability.

## Requirements

### Requirement: Automatic update check on startup

The system SHALL check for updates asynchronously when the application starts.

#### Scenario: Update check at startup
- **WHEN** the application starts
- **THEN** an update check runs in the background without blocking the UI

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
