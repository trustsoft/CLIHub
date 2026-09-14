## Purpose

Owns reading and writing the application's `config.json` so that user settings and
registered projects survive restarts without losing unknown or hand-edited data.

## ADDED Requirements

### Requirement: Load configuration with defaults

The system SHALL load `config.json`; when the file is missing or cannot be parsed,
the system SHALL fall back to built-in defaults.

#### Scenario: Missing file

- **WHEN** `config.json` does not exist
- **THEN** the system uses built-in defaults

#### Scenario: Unparseable file

- **WHEN** `config.json` exists but cannot be parsed
- **THEN** the system uses built-in defaults and does not crash

### Requirement: Persist configuration without data loss

The system SHALL write `config.json` when the configuration changes and SHALL
preserve any fields it does not model.

#### Scenario: Unknown fields survive a round-trip

- **WHEN** `config.json` contains fields the application does not model and the configuration is saved
- **THEN** those fields remain present in the written file

### Requirement: Atomic replacement

The system SHALL replace `config.json` atomically so that a partially written file
is never observable.

#### Scenario: Save does not leave a partial file

- **WHEN** the configuration is saved
- **THEN** `config.json` contains either the previous content or the new content, never a partial write

### Requirement: Back up a corrupt configuration

The system SHALL, before overwriting a `config.json` that failed to parse, copy the
previous content aside as `config.json.bak`.

#### Scenario: Corrupt file backed up before overwrite

- **WHEN** `config.json` could not be parsed and the system saves the configuration
- **THEN** the previous file is copied to `config.json.bak` before the new content is written
