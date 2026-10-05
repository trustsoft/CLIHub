## MODIFIED Requirements

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
