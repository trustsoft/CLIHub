## ADDED Requirements

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

## MODIFIED Requirements

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
