## MODIFIED Requirements

### Requirement: Updates page options

The Settings page header for "Updates" SHALL show the current version and stable channel as chips next to the page title, an update-status chip indicating either that the build is current or that an update is available, a "Check for updates" action, and a "View release notes" action aligned to the bottom-right of the header. The page body SHALL offer the "Check for updates on startup", "Automatically download updates", "Notify about stable releases", and "Notify about pre-release versions" controls under an "UPDATE PREFERENCES" section. The page SHALL show the current application version and check status without commit, target, or agent-protocol detail.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "Updates"
- **THEN** the page header shows the version and stable channel chips, the update-status chip, and the "Check for updates" action
- **AND** the bottom-right of the header shows "View release notes"
- **AND** the page body shows "Check for updates on startup", "Automatically download updates", "Notify about stable releases", and "Notify about pre-release versions" toggles under "UPDATE PREFERENCES"

#### Scenario: Defaults

- **WHEN** no update preferences have been stored
- **THEN** "Automatically download updates" and "Notify about stable releases" are on and "Notify about pre-release versions" is off

#### Scenario: Persist on save

- **WHEN** the user changes an update toggle and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Version status simplified

- **WHEN** the Updates page is shown
- **THEN** the status shows the current version and the check status (up to date, update available with the version, failed, cancelled, or not installed) without commit, target, or agent-protocol detail

#### Scenario: Header status chip reports the check outcome

- **WHEN** a check started from the page header completes
- **THEN** the header status chip reflects the outcome
- **AND** an available update is reported as "Update available"
- **AND** the chip detail includes the current and available versions

#### Scenario: Pre-existing update availability

- **WHEN** an update was already found before the Settings page is opened
- **THEN** the header status chip shows that an update is available

## ADDED Requirements

### Requirement: Release notes from Settings

The Updates page SHALL let the user open the application release notes from the status card.

#### Scenario: Open release notes

- **WHEN** the user activates "View release notes"
- **THEN** the What's New window opens, or the existing one is brought to the front
- **AND** the running version is recorded as seen using the existing release-notes launcher behavior
