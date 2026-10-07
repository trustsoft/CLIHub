## Why

The configuration repository already returns detached snapshots, but its mutable owner is still an `AppConfig` domain object and the persistence document serializes `Project` and `AppPreferences` directly. This leaves the application state, persistence DTO, and mapping responsibilities coupled at the JSON boundary.

## What Changes

- Make `ConfigurationSnapshot` the repository's single mutable state owner.
- Introduce explicit persistence DTOs and mapping for every project, preference, and root field.
- Preserve the existing flat `config.json` shape, schema migration behavior, debounce/Flush semantics, and atomic replacement.
- Add mapping and concurrent/latest-snapshot tests.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This is an internal configuration-boundary refactor; `skip_specs: true` is set because the persisted format and user behavior remain unchanged.

## Impact

- Affects `ConfigurationRepository`, persistence DTOs/mapping, configuration snapshots, and Core configuration tests.
- Existing migration and writer infrastructure remains the owner of schema/version and atomic file mechanics.
