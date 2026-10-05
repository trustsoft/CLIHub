## ADDED Requirements

### Requirement: Configuration reads are mutation-isolated

The configuration boundary SHALL return a snapshot whose projects, preferences, and nested mutable values are detached from service-owned state.

#### Scenario: Mutating a returned snapshot does not change stored state
- **GIVEN** the configuration contains a project and preferences
- **WHEN** a caller mutates its local snapshot or any nested project/preferences value without submitting an update
- **THEN** a subsequent configuration read returns the original stored values

### Requirement: Configuration updates are explicit

The configuration boundary SHALL persist changes only when a caller submits an explicit updated snapshot or update operation.

#### Scenario: Explicit update persists changed state
- **GIVEN** a caller has read a configuration snapshot
- **WHEN** the caller creates an updated snapshot and submits it
- **THEN** the updated projects, preferences, and current project are persisted

### Requirement: Snapshot persistence preserves the existing document

The snapshot boundary SHALL preserve the existing single `config.json` document, JSON property names, default values, debounced writes, atomic replacement, and synchronous flush guarantees.

#### Scenario: Snapshot round-trip preserves configuration data
- **GIVEN** a snapshot containing projects, preferences, and a current project ID
- **WHEN** it is persisted, flushed, and loaded by a new configuration service instance
- **THEN** all values round-trip without changing the established JSON document shape

### Requirement: Stores use the snapshot boundary

The preferences and project state stores SHALL load and submit configuration snapshots without mutating a shared configuration graph.

#### Scenario: Store update preserves unrelated state
- **GIVEN** the configuration contains both preferences and project state
- **WHEN** a store updates only its owned portion
- **THEN** the updated portion is persisted and unrelated portions retain their previous values
