## ADDED Requirements

### Requirement: Updates page options

The Updates page SHALL offer the "Check for updates on startup" toggle, the manual update check, and the toggles "Automatically download updates", "Notify about stable releases", and "Notify about pre-release versions", and SHALL show the current application version together with the check status.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Updates"
- **THEN** it shows "Check for updates on startup", "Automatically download updates", "Notify about stable releases", and "Notify about pre-release versions" toggles, and the manual update check

#### Scenario: Defaults
- **WHEN** no update preferences have been stored
- **THEN** "Automatically download updates" and "Notify about stable releases" are on and "Notify about pre-release versions" is off

#### Scenario: Persist on save
- **WHEN** the user changes an update toggle and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Version status simplified
- **WHEN** the Updates page is shown
- **THEN** the version status shows the current version and the check status (up to date, or update available with the version) without commit, target, or agent-protocol detail
