## Why

`ConfigService.Load()` currently returns the cached mutable `AppConfig` graph, and the preferences and project stores mutate nested objects before calling `Save`. This allows callers to change shared configuration state without an explicit ownership boundary and makes concurrent/background persistence harder to reason about.

## What Changes

- Introduce an immutable or mutation-isolated `ConfigurationSnapshot` representation for configuration reads.
- Make snapshot creation deep-copy nested preferences, projects, and mutable project data so callers cannot mutate the service-owned state through returned references.
- Add an explicit way to create a writable configuration update from a snapshot and persist the resulting state.
- Adapt `PreferencesStore`, `ProjectStateStore`, and `ConfigService` to use snapshot boundaries while preserving the single `config.json` document and existing JSON shape.
- Preserve existing debounce, atomic write, flush, and compatibility behavior while making ownership explicit.
- Add tests proving snapshot isolation, round-trip preservation, and compatibility with current stores.

## Capabilities

### New Capabilities

- `configuration-snapshot`: Defines isolated configuration snapshots, explicit update/persistence boundaries, and preservation of the existing configuration document.

### Modified Capabilities

None.

## Impact

- Affected Core models and configuration contracts: `AppConfig`, `IConfigService`, `ConfigService`, `PreferencesStore`, and `ProjectStateStore`.
- Existing consumers and test fakes implementing `IConfigService` will need migration to the new snapshot contract.
- Persistence remains at `%APPDATA%\\CLIHub\\config.json` with the current flat JSON document and atomic writer.
- This is a Core API change; WPF application behavior should remain unchanged after adapters are migrated.
