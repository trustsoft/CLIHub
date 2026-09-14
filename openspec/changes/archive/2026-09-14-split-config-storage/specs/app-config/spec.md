## MODIFIED Requirements

### Requirement: Load configuration with defaults

The system SHALL load each configuration document file it owns; when a
document file is missing or cannot be parsed, the system SHALL fall back to
built-in defaults for that document.

#### Scenario: Missing file

- **WHEN** a document file does not exist
- **THEN** the system uses built-in defaults for that document

#### Scenario: Unparseable file

- **WHEN** a document file exists but cannot be parsed
- **THEN** the system uses built-in defaults for that document and does not crash

### Requirement: Persist configuration without data loss

The system SHALL write a document file when that document changes and SHALL
preserve any fields it does not model within the same document file.

#### Scenario: Unknown fields survive a round-trip

- **WHEN** a document file contains fields the application does not model and the document is saved
- **THEN** those fields remain present in the written file

### Requirement: Atomic replacement

The system SHALL replace each document file atomically so that a partially
written file is never observable.

#### Scenario: Save does not leave a partial file

- **WHEN** a document is saved
- **THEN** the file contains either the previous content or the new content, never a partial write

### Requirement: Back up a corrupt configuration

The system SHALL, before overwriting a document file that failed to parse,
copy the previous content aside with a `.bak` suffix.

#### Scenario: Corrupt file backed up before overwrite

- **WHEN** a document file could not be parsed and the system saves that document
- **THEN** the previous file is copied to `<file>.bak` before the new content is written

## ADDED Requirements

### Requirement: Migrate legacy single-file configuration

The system SHALL, on load, when the legacy `config.json` exists and
`settings.json` does not, split the legacy file into `settings.json`,
`projects.json`, and `agents.json` according to the owner layout, and keep
the original aside as `config.json.migrated`. The split SHALL run once:
when `settings.json` already exists, a present legacy `config.json` SHALL be
ignored.

#### Scenario: First launch after upgrade

- **WHEN** `config.json` exists and `settings.json` does not
- **THEN** the three owner files are written with the corresponding legacy sections and `config.json` is kept as `config.json.migrated`

#### Scenario: Unknown top-level fields survive the split

- **WHEN** the legacy `config.json` contains top-level fields the application does not model
- **THEN** those fields are carried into `settings.json`

#### Scenario: Split is one-time

- **WHEN** `settings.json` already exists and a legacy `config.json` is also present
- **THEN** no split runs and the current owner files are used unchanged
