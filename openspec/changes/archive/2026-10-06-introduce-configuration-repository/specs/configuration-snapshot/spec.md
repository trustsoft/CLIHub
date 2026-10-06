## MODIFIED Requirements

### Requirement: Configuration updates are explicit

The configuration repository SHALL persist changes only after a complete update transaction and SHALL apply each transaction to the latest committed snapshot.

#### Scenario: Explicit update persists changed state
- **GIVEN** a caller has read a configuration snapshot
- **WHEN** the caller submits a complete update operation
- **THEN** the updated projects, preferences, and current project are persisted

#### Scenario: Failed store update does not persist partial state
- **GIVEN** an owning store has stored preferences or project state
- **WHEN** an explicit update callback throws before completion
- **THEN** the repository propagates the error and the previous stored state remains unchanged

#### Scenario: Concurrent updates preserve both completed changes
- **GIVEN** the repository contains a configuration snapshot
- **WHEN** concurrent update operations change different configuration fields
- **THEN** each completed update is applied to the latest committed snapshot and neither update is lost

### MODIFIED Requirement: Snapshot persistence preserves the existing document

The configuration repository SHALL be the sole coordination boundary for snapshots, schema migrations, debounced persistence, atomic replacement, and synchronous flush of the existing single `config.json` document, while preserving its JSON property names, defaults, and current schema-version metadata.

#### Scenario: Snapshot round-trip preserves configuration data
- **GIVEN** a snapshot containing projects, preferences, and a current project ID
- **WHEN** it is persisted, flushed, and loaded by a new repository instance
- **THEN** all values round-trip without changing the established JSON document shape

#### Scenario: New writes identify the current schema
- **GIVEN** a configuration is persisted by the current application
- **WHEN** the resulting `config.json` is read
- **THEN** it contains the current numeric `schemaVersion` at the top level

#### Scenario: Successful migration schedules the current schema document
- **GIVEN** a supported legacy configuration is migrated successfully
- **WHEN** the repository completes loading the migrated snapshot
- **THEN** it schedules the migrated configuration with the current schema version for persistence

#### Scenario: Failed migration preserves the original document
- **GIVEN** a legacy configuration migration fails
- **WHEN** the repository handles the failed load
- **THEN** it does not publish or persist a partially migrated snapshot and leaves the original document unchanged

#### Scenario: Flush persists all completed updates
- **GIVEN** one or more repository updates have completed
- **WHEN** the repository's synchronous flush operation returns
- **THEN** all updates completed before the flush began are durable in the single configuration document

#### Scenario: Repository retains atomic single-file persistence
- **GIVEN** configuration updates are pending
- **WHEN** the repository writes them to disk
- **THEN** it preserves the established JSON property shape and uses debounced atomic replacement of the single `config.json` file
