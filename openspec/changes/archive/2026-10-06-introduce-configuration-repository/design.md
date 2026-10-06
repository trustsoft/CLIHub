## Context

The current `ConfigService` owns cached `AppConfig` state, schema migration invocation, debounce coordination, atomic file replacement, and flush. Store implementations depend on `IConfigService`, read detached snapshots, mutate them, then save. Although stores now expose explicit callback updates, the read and save still occur as separate repository operations, so concurrent updates can overwrite one another.

The repository change follows snapshot isolation, explicit store updates, schema versioning, and the migration runner. It consolidates those capabilities under one application-facing contract without changing the single `config.json` format.

## Goals / Non-Goals

**Goals:**

- Establish one `IConfigurationRepository` boundary for detached snapshot reads, serialized update transactions, migrations, and persistence lifecycle.
- Apply each update to the latest committed snapshot so concurrent unrelated updates are not lost.
- Keep preferences/project stores scoped to their owned sections while using repository transactions.
- Preserve schema migration ordering, non-destructive migration failure behavior, and successful migration normalization.
- Preserve debounce, atomic replacement, synchronous flush, and the existing single-file JSON contract.
- Remove the split `IConfigService`/`ConfigService` boundary after migrating all callers, fakes, DI registrations, and tests.

**Non-Goals:**

- Change the persisted field set or add another schema migration.
- Implement separate persistence backends or multi-file configuration.
- Add cross-process locking or optimistic version-conflict UI.
- Change user-visible settings, project, or runtime behavior.

## Decisions

- **Use one application contract for the complete lifecycle.** Replace `IConfigService` with `IConfigurationRepository`, exposing detached `Read()` results, `Update(Action<ConfigurationSnapshot>)`, and `Flush()`. Keep one concrete `ConfigurationRepository` as the disk-backed implementation. This is preferred over retaining a compatibility `IConfigService` adapter because two writable contracts would obscure ownership and invite a second writer path.
- **Make `Update` a serialized transaction.** The repository clones its latest committed snapshot, runs the synchronous callback against that clone, then publishes and queues the completed result. A callback exception propagates without changing committed state or pending JSON. A single update gate is preferred over optimistic retries because callbacks may have side effects and cannot safely be replayed.
- **Keep store ownership narrow.** Preferences and project stores continue to expose read/update operations, but their callbacks execute inside repository transactions and modify only their owned section. They do not retain snapshots or call a separate Save operation.
- **Run migrations as part of lazy repository initialization.** Current schema is a no-op. A supported older schema is migrated fully in memory; only a complete result is published and queued as the normalized current-version document. Missing migration paths and migration exceptions leave the source file untouched and expose safe defaults to the running process.
- **Retain one debounced writer and atomic replacement.** The repository owns serialization, debounce, `.tmp` write/replace, and `Flush`. Keeping those responsibilities in one implementation is preferred over separate writer services because there is one document and one ordering contract.
- **Define the flush concurrency boundary.** Updates completed before `Flush()` begins must be durable when it returns. An update overlapping a flush is ordered by the repository gate and belongs either to that flush or to the next one.
- Register the repository as a singleton. Migration implementations remain independent Core registrations and are composed by `ConfigMigrationRunner`.

## Risks / Trade-offs

- **Callbacks hold update serialization while running** -> Keep callbacks synchronous, small, and limited to in-memory mutation; add tests that callbacks are not published on exception.
- **Successful migration is persisted during initialization** -> Queue normalization only after the complete migration path succeeds; rely on the existing atomic writer and never rewrite on migration failure.
- **Contract replacement touches many callers and fakes** -> Migrate through compiler-checked solution builds and require all projects to resolve the same singleton repository.
- **Flush overlaps an update** -> Define and test ordering at the repository gate; require durability for updates completed before Flush begins, without promising a barrier for future concurrent updates.

## Migration Plan

1. Introduce `IConfigurationRepository` and the repository implementation, transferring snapshot ownership and persistence responsibilities from `ConfigService`.
2. Integrate schema validation and migrations, including scheduling successful migration normalization.
3. Migrate preferences/project stores, Core and WPF composition, callers, and test fakes; remove `IConfigService`.
4. Add concurrency tests proving serialized updates preserve unrelated changes, callback failures do not publish, migrations normalize safely, and flush drains completed updates.
5. Run focused Core configuration tests, full Release build/test, and OpenSpec validation.
6. Rollback consists of restoring `IConfigService`/`ConfigService` and the prior store wiring; `config.json` remains compatible.

## Open Questions

None.
