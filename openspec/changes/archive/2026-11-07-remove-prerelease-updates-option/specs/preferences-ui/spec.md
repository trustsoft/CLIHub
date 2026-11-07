# preferences-ui (delta)

## Modified Requirements

### Requirement: Updates page options

The Settings page header for "Updates" SHALL show the current version as a chip next to the page title, an update-status chip indicating either that the build is current or that an update is available, a "Check for updates" action, and a "Release notes" action on the title row after the version chip. The page body SHALL offer the "Check for updates on startup", "Automatically download updates", and "Notify about stable releases" controls. The page SHALL show the current application version and check status without commit, target, or agent-protocol detail.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "Updates"
- **THEN** the page header shows the version chip, the update-status chip, and the "Check for updates" action
- **AND** the title row shows "Release notes" after the version chip
- **AND** the page body shows "Check for updates on startup", "Automatically download updates", and "Notify about stable releases" toggles

#### Scenario: Defaults

- **WHEN** no update preferences have been stored
- **THEN** "Automatically download updates" and "Notify about stable releases" are on

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
