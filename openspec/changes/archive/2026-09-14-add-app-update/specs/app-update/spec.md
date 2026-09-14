# app-update Delta

## Purpose

Keeps the installed CLIHub up to date by checking for, downloading, and
applying new releases through a Velopack feed, without blocking the UI and
without disturbing users who run a non-installed development build.

## ADDED Requirements

### Requirement: Update settings in config

The system SHALL store update settings in the `update` section of
`config.json`, with `checkOnStartup` (boolean, default true) controlling
whether the startup check runs.

#### Scenario: Missing section uses the default

- **WHEN** `config.json` has no `update` section
- **THEN** `checkOnStartup` is treated as true

#### Scenario: Disabled startup check

- **WHEN** `config.json` sets `update.checkOnStartup` to false
- **THEN** no update check runs at startup

### Requirement: Startup update check

The system SHALL run an update check at application startup in the
background, without blocking the UI, when `update.checkOnStartup` is
enabled.

#### Scenario: Check runs in the background

- **WHEN** the application starts with `checkOnStartup` enabled
- **THEN** an update check runs and the popup stays responsive

#### Scenario: No update available

- **WHEN** the check finds no newer release
- **THEN** nothing is downloaded and the user is not notified

### Requirement: Background download of a newer release

The system SHALL, when the check finds a newer release, download it in the
background. Only after a successful download SHALL the update be considered
ready.

#### Scenario: Newer release downloaded

- **WHEN** a newer release exists and the download succeeds
- **THEN** the update becomes ready for the user to apply

#### Scenario: Download failure

- **WHEN** a newer release exists but the download fails
- **THEN** the failure is silent and no notification is shown

### Requirement: Tray notification when an update is ready

The system SHALL notify the user through the system tray once a downloaded
update is ready, and SHALL offer a user action that applies it.

#### Scenario: Notification after download

- **WHEN** a downloaded update becomes ready
- **THEN** a tray notification informs the user an update is available

### Requirement: Apply and restart on user action

The system SHALL, when the user acts on the ready update, install the
downloaded release and restart the application. The system SHALL NOT apply
an update without an explicit user action.

#### Scenario: Applying the update

- **WHEN** the user acts on the update notification
- **THEN** the downloaded release is installed and the application restarts

#### Scenario: No automatic application

- **WHEN** a downloaded update is ready and the user does not act
- **THEN** the update is not applied and the application keeps running the current version

### Requirement: Silent operation outside an installed build

The system SHALL skip update checks silently when the application is not a
Velopack-installed build.

#### Scenario: Development build

- **WHEN** the application runs from a development build instead of an installed release
- **THEN** no update check runs and no error is shown

### Requirement: Silent failure and retry

The system SHALL treat check failures (offline, unreachable feed) as
silent: no blocking or error dialog is shown, and the next startup retries.

#### Scenario: Offline at startup

- **WHEN** the update check cannot reach the feed
- **THEN** nothing is shown to the user and the application works normally

#### Scenario: Retried later

- **WHEN** a previous startup check failed
- **THEN** the next startup checks again
