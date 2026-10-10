## ADDED Requirements

### Requirement: Update preferences default safely

The configuration boundary SHALL default the update preferences when they are absent from a stored document, and SHALL round-trip them: `autoDownloadUpdates` default `true`, `notifyStableUpdates` default `true`, and `notifyPrereleaseUpdates` default `false`.

#### Scenario: Absent fields use defaults
- **GIVEN** a stored configuration has no update-preference fields
- **WHEN** the configuration is loaded
- **THEN** auto-download and stable notifications are enabled and pre-release notifications are disabled

#### Scenario: Update preferences round-trip
- **GIVEN** the update preferences are set to specific values
- **WHEN** the configuration is persisted and loaded again
- **THEN** each preference retains its value
