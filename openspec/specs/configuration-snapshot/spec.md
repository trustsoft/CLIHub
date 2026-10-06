# configuration-snapshot Specification

## Purpose

Provides an isolated configuration boundary so callers can read and update application state without mutating a shared in-memory graph implicitly, while preserving the established config.json persistence contract.

## Requirements

### Requirement: Configuration reads are mutation-isolated

The configuration boundary SHALL return a snapshot whose projects, preferences, and nested mutable values are detached from service-owned state.

#### Scenario: Mutating a returned snapshot does not change stored state
- **GIVEN** the configuration contains a project and preferences
- **WHEN** a caller mutates its local snapshot or any nested project/preferences value without submitting an update
- **THEN** a subsequent configuration read returns the original stored values

### Requirement: Configuration updates are explicit

The configuration boundary SHALL persist changes only when a caller submits an explicit updated snapshot or an owning store completes an explicit update operation successfully.

#### Scenario: Explicit update persists changed state
- **GIVEN** a caller has read a configuration snapshot
- **WHEN** the caller creates an updated snapshot and submits it
- **THEN** the updated projects, preferences, and current project are persisted

#### Scenario: Failed store update does not persist partial state
- **GIVEN** an owning store has stored preferences or project state
- **WHEN** an explicit update callback throws before completion
- **THEN** the store propagates the error and the previous stored state remains unchanged

### Requirement: Snapshot persistence preserves the existing document

The snapshot boundary SHALL preserve the existing single `config.json` document, JSON property names, default values, debounced writes, atomic replacement, and synchronous flush guarantees, while writing the current schema version as top-level metadata.

#### Scenario: Snapshot round-trip preserves configuration data
- **GIVEN** a snapshot containing projects, preferences, and a current project ID
- **WHEN** it is persisted, flushed, and loaded by a new configuration service instance
- **THEN** all values round-trip without changing the established JSON document shape

#### Scenario: New writes identify the current schema
- **GIVEN** a configuration is persisted by the current application
- **WHEN** the resulting `config.json` is read
- **THEN** it contains the current numeric `schemaVersion` at the top level

### Requirement: Stores use the snapshot boundary

The preferences and project state stores SHALL expose read operations and explicit update operations that work on detached owned values, and SHALL persist the owned section without requiring callers to pair mutable `Load` and `Save` calls.

#### Scenario: Store update preserves unrelated state
- **GIVEN** the configuration contains both preferences and project state
- **WHEN** a store updates only its owned portion through its explicit update operation
- **THEN** the updated portion is persisted and unrelated portions retain their previous values

#### Scenario: Read values are detached from the store
- **GIVEN** a caller reads preferences or project state
- **WHEN** the caller mutates the returned value without invoking an explicit update operation
- **THEN** a subsequent read returns the previously stored value

### Requirement: Configuration schema versions are explicit and compatible

The configuration boundary SHALL treat a missing `schemaVersion` as legacy version 0, accept it through the established compatibility path, and distinguish unsupported future or invalid versions from the current schema without silently overwriting their source document.

#### Scenario: Legacy document without a version remains readable
- **GIVEN** a valid existing configuration document has no `schemaVersion` property
- **WHEN** the configuration is loaded
- **THEN** its projects and preferences are loaded using the legacy compatibility behavior

#### Scenario: Unknown future version is non-destructive
- **GIVEN** a configuration document declares a schema version greater than the current version
- **WHEN** the configuration is loaded
- **THEN** the service reports the unsupported schema, uses safe defaults for the running process, and does not overwrite the source document

#### Scenario: Invalid version is rejected safely
- **GIVEN** a configuration document declares an invalid schema version
- **WHEN** the configuration is loaded
- **THEN** the service follows the safe-default/error logging path and does not perform a destructive rewrite

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
