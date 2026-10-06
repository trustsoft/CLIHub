## Why

Configuration ownership and persistence are currently split across `ConfigService`, migration handling, and the preferences/project stores. A single repository boundary is needed to own the snapshot lifecycle and serialize updates so concurrency, migrations, debounce, atomic writes, and flush have one coherent contract.

## What Changes

- Introduce `IConfigurationRepository` as the single application boundary for configuration snapshots and updates.
- Move ownership of the current snapshot, schema migrations, serialized update operations, debounced persistence, atomic replacement, and flush behind the repository.
- Migrate `ConfigService`, `IConfigService`, preferences/project stores, dependency injection, and test fakes/callers to the repository boundary.
- Ensure concurrent updates are applied against the latest repository state and do not overwrite unrelated updates.
- Preserve the existing single `config.json`, schema-version behavior, migration failure policy, and durable-write semantics.
- Add repository contract, concurrency, migration, persistence-failure, and flush tests.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `configuration-snapshot`: Make the repository the owner of configuration snapshots, migrations, serialized updates, persistence, and flush guarantees.

## Impact

- Affected Core configuration services/contracts, migration integration, preferences/project stores, Core DI, WPF composition consumers where configuration contracts are resolved, and tests.
- `config.json` format and path remain unchanged; no user-data conversion beyond existing schema migrations is introduced.
- This is a source-level architecture change within the repository. User-visible settings and project behavior remain unchanged.
