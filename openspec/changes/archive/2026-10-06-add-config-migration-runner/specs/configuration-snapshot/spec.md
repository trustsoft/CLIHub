## ADDED Requirements

### Requirement: Configuration migrations are ordered and explicit

The configuration boundary SHALL apply registered migrations in an ordered, contiguous sequence from the loaded schema version to the current schema version, while treating current-version documents as a no-op.

#### Scenario: Current-version document requires no migration
- **GIVEN** a configuration document declares the current schema version
- **WHEN** the configuration is loaded
- **THEN** no migration is invoked and the document is converted normally

#### Scenario: Legacy document follows the migration path
- **GIVEN** a configuration document declares a supported older schema version and migrations cover every step to the current version
- **WHEN** the configuration is loaded
- **THEN** migrations run once in ascending version order before the configuration is converted

### Requirement: Migration failures are non-destructive

The configuration boundary SHALL treat missing migration paths and migration exceptions as load failures, preserve the source document, and avoid automatically writing a partially migrated result.

#### Scenario: Missing migration path does not overwrite the document
- **GIVEN** a configuration document requires a migration for which no contiguous path is registered
- **WHEN** the configuration is loaded
- **THEN** the service reports the migration failure, uses safe defaults for the running process, and leaves the source document unchanged

#### Scenario: Failed migration does not publish partial state
- **GIVEN** an ordered migration sequence where one migration throws after changing its in-memory input
- **WHEN** the configuration is loaded
- **THEN** the service reports the failure, does not publish the partial result, and leaves the source document unchanged
