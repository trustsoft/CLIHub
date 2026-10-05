## Why

Configuration compatibility currently depends on default property values and silent JSON handling. That is insufficient when a future release changes field meaning or removes properties, because the application cannot distinguish a legacy document from a newer unsupported schema.

## What Changes

- Add an explicit `schemaVersion` field to the persisted configuration document.
- Define the current schema version and the legacy version represented by a missing field.
- Write the current schema version for every newly persisted configuration.
- Accept legacy documents without `schemaVersion` through the current compatibility path.
- Detect unknown future schema versions and apply a non-destructive unsupported-schema policy instead of silently treating them as current data.
- Add tests for current-version writes, legacy reads, unknown-version handling, and preservation of the existing flat document shape.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `configuration-snapshot`: Add explicit schema-version metadata and compatibility rules for legacy and unsupported configuration documents.

## Impact

- Affected Core persistence mapping and configuration loading: `AppConfigDocument`, `ConfigService`, and related serialization tests.
- Existing config files without `schemaVersion` remain readable as legacy version 0.
- New writes add `schemaVersion` while retaining all existing property names and the single atomic `config.json` path.
- Configuration migration execution remains out of scope and is reserved for the following migration-runner changes.
