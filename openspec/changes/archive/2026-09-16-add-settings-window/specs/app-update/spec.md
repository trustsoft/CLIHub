# app-update Delta

## ADDED Requirements

### Requirement: Manual update check from the settings window

The system SHALL run an update check when the user triggers the check action
in the settings window, regardless of the `checkOnStartup` setting, and SHALL
report the outcome of a user-initiated check inside the settings window. The
silent-check rules for startup checks remain unchanged.

#### Scenario: Up to date

- **WHEN** the user triggers a check and no newer release exists
- **THEN** the settings window reports that the application is up to date

#### Scenario: Update found and downloaded

- **WHEN** the user triggers a check and a newer release downloads
  successfully
- **THEN** the settings window reports the update is ready to install, and the
  update is applied only through the explicit user action in the tray

#### Scenario: Check fails

- **WHEN** the user triggers a check and it cannot reach the feed
- **THEN** the settings window reports the failure, and the check action
  becomes available again

#### Scenario: Not an installed build

- **WHEN** the user triggers a check while the application is not a
  Velopack-installed build
- **THEN** the settings window reports that update checks require an installed
  build
