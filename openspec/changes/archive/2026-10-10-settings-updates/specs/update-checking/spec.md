## ADDED Requirements

### Requirement: Background update download

The system SHALL, when background auto-download is enabled, download an available update automatically after a check finds one, and SHALL expose the resulting downloading and restart-to-update states through the shared update workflow. When the preference is disabled, the update SHALL be offered as a manual download action instead.

#### Scenario: Auto-download when enabled
- **WHEN** the background auto-download preference is enabled and a check finds an update
- **THEN** the download starts automatically and the update surfaces show the downloading state

#### Scenario: Manual download when disabled
- **WHEN** the background auto-download preference is disabled and a check finds an update
- **THEN** no download starts until the user invokes the download action

#### Scenario: Automatic download failure falls back
- **WHEN** an automatic download fails
- **THEN** the application keeps running the current version and the download action remains available

#### Scenario: Restart offered after automatic download
- **WHEN** the automatic download completes successfully
- **THEN** the update surfaces offer the restart-to-update action

### Requirement: Update notification preferences

The system SHALL notify about stable and pre-release updates according to two independent preferences, and SHALL include pre-release versions in the update check only when pre-release notifications are enabled.

#### Scenario: Stable notifications enabled
- **WHEN** a stable update is found and stable notifications are enabled
- **THEN** the update availability notification is shown

#### Scenario: Stable notifications disabled
- **WHEN** a stable update is found and stable notifications are disabled
- **THEN** no update availability notification is shown

#### Scenario: Pre-release versions included
- **WHEN** pre-release notifications are enabled
- **THEN** the update check includes pre-release versions

#### Scenario: Pre-release notification shown
- **WHEN** a pre-release update is found and pre-release notifications are enabled
- **THEN** the update availability notification is shown

#### Scenario: Both notification preferences disabled
- **WHEN** both notification preferences are disabled
- **THEN** no update availability notification is shown
