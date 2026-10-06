## Context

`ConfigService` currently reads `AppConfigDocument`, accepts legacy version 0 or current version 1, and falls back to defaults for unsupported versions. The next configuration changes need a controlled place to transform a document from one schema version to the next without coupling individual migrations to file I/O or ordinary model deserialization.

The runner must be introduced before `migrate-legacy-terminal-preference`, so this change defines the execution contract and wiring but does not register that concrete migration.

## Goals / Non-Goals

**Goals:**

- Provide a deterministic, ordered migration pipeline from a source schema version to the current version.
- Keep migrations pure over a persistence document or equivalent migration model and independent of disk I/O.
- Make migration failures observable and non-destructive.
- Preserve current schema-version handling and atomic/debounced persistence behavior.
- Make the runner straightforward to unit test with fake migrations.

**Non-Goals:**

- Implement `terminalExecutable -> defaultRuntime`.
- Add schema versions beyond the currently supported target unless required by the runner contract.
- Change unknown-future-version handling.
- Introduce the final configuration repository abstraction.

## Decisions

- Add `IConfigMigration` with source version, target version, and a method that transforms an `AppConfigDocument` or migration document in memory.
- Add `ConfigMigrationRunner` that receives an ordered migration collection, validates a contiguous path, and applies each migration exactly once in ascending version order.
- The runner returns the migrated document and final version; it does not write files or mutate `ConfigurationSnapshot` directly.
- A current-version document is a no-op. A legacy document with no registered path remains readable only when no transformation is required; once a migration is registered, the runner applies it before model conversion.
- If no contiguous migration path exists, or a migration throws, loading fails safely, logs the failure, and does not automatically overwrite the source file. The existing source file remains authoritative until a later successful explicit save.
- Register the runner through Core composition as a singleton, with an empty migration collection until the next concrete migration change adds one.
- Keep unknown future versions outside the runner path and preserve the existing non-destructive policy.

## Risks / Trade-offs

- **Migration ordering is ambiguous** -> Require exact source/target versions and reject duplicate or branching steps.
- **Partial migration mutates shared state** -> Run against an in-memory document copy and publish only a completed result.
- **Empty migration collection changes legacy behavior** -> Keep version 0 compatibility unchanged until a concrete migration is registered.

## Migration Plan

1. Define migration document/contracts and runner result/error semantics.
2. Integrate runner selection into `ConfigService` loading without registering a concrete migration.
3. Register the empty runner in Core DI and add fake migration tests.
4. Run full build/test and validate/sync the configuration snapshot spec.
5. Follow with `migrate-legacy-terminal-preference` to add the first concrete migration.

## Open Questions

None.
