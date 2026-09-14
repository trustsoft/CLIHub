# app-settings Specification

## Purpose

Owns the user-editable global settings file (`settings.json`) so that
application-wide preferences survive restarts independently of machine state
and project data.

## Requirements

### Requirement: Single user settings document

The system SHALL store user-editable global settings in `settings.json` in
the application data directory. The document SHALL contain the default
`runtime`, the `hotkey` combination, the `probe` tuning, and the `update`
check settings. Project entries and the agent probe cache SHALL NOT be
stored in this document.

#### Scenario: Settings saved to their own file

- **WHEN** global settings are saved
- **THEN** `settings.json` contains `runtime`, `hotkey`, `probe`, and `update` data

#### Scenario: Machine and project data stay out

- **WHEN** the application persists a probe round or a project change
- **THEN** `settings.json` is not modified

### Requirement: Defaults when settings are absent

The system SHALL apply built-in defaults for every settings value that is
missing, so that a missing or unparseable `settings.json` leaves the
application fully functional.

#### Scenario: Missing settings file

- **WHEN** `settings.json` does not exist or cannot be parsed
- **THEN** default runtime, hotkey, probe tuning, and update check settings apply and the application runs normally
