## Context

The configuration service owns one cached `AppConfig`, but `Load()` exposes that mutable object directly. `PreferencesStore` replaces `config.Preferences`, while `ProjectStateStore` mutates the loaded configuration through `ProjectState.ApplyTo`. The config writer serializes on `Save`, which prevents background serialization races but does not prevent accidental mutation of the cached graph between explicit saves.

The preceding concurrency-test change establishes the persistence guarantees that must remain true while the ownership model changes. Later changes will make store updates even more explicit and introduce schema migrations, so this snapshot boundary must be narrow and composable.

## Goals / Non-Goals

**Goals:**

- Make configuration reads isolated from service-owned mutable state.
- Keep mutation and persistence explicit and compatible with the current stores.
- Deep-copy all mutable nested state that can be reached from a configuration snapshot.
- Preserve current serialization format, default values, atomic writes, debounce, and flush semantics.
- Provide a migration path for existing `IConfigService` implementations and test fakes.

**Non-Goals:**

- Add schema versioning or migration execution; those are later changes.
- Introduce the final `IConfigurationRepository`; this change establishes the snapshot primitive and boundary only.
- Make every domain model globally immutable in one step.
- Change preferences or project behavior visible to the WPF application.

## Decisions

- Add a Core-owned `ConfigurationSnapshot` type representing the complete persisted configuration state. Its public state must not expose mutable collections or mutable model instances that can alter the service-owned snapshot.
- Use immutable/read-only collections and immutable value-like representations where practical. For existing mutable `Project` and `AppPreferences` models, provide defensive copies at the boundary until those models are migrated separately.
- Replace the ambiguous `IConfigService.Load`/`Save(AppConfig)` pairing with explicit snapshot-oriented operations. The exact method names should follow existing naming conventions, but the contract must distinguish reading a snapshot from persisting a new snapshot.
- Keep compatibility adapters internal where necessary so stores and test doubles migrate without reintroducing shared mutable state.
- `ConfigService` may retain an internal mutable representation for serialization, but every read returns a detached snapshot and every write consumes a detached update.
- `PreferencesStore` and `ProjectStateStore` load a snapshot, construct an updated snapshot, and submit it explicitly; they must not mutate a reference returned by the service.
- Preserve `AppConfigDocument` as the persistence mapping layer and preserve the current JSON property names and one-file atomic write path.

## Risks / Trade-offs

- **Broad interface migration** -> Update all Core callers and test fakes in one change; keep compatibility only at private/internal seams.
- **Deep-copy omissions** -> Add tests that mutate returned projects/preferences and verify a subsequent read is unchanged until an explicit update is submitted.
- **Extra allocations** -> Accept snapshot copies for the small configuration document; correctness of ownership is the priority.
- **Accidental JSON shape change** -> Reuse `AppConfigDocument` and existing serialization tests.

## Migration Plan

1. Define `ConfigurationSnapshot` and conversion/copy helpers for the existing models.
2. Update `IConfigService` and `ConfigService` to expose detached snapshots and explicit writes.
3. Migrate `PreferencesStore`, `ProjectStateStore`, Core test fakes, and all callers.
4. Add isolation and round-trip tests, then run the full Core and WPF test suites.
5. Rollback consists of restoring the current `AppConfig`-based interface and adapters; no config-file migration is required.

## Open Questions

- Whether the snapshot should be a positional record or a named class can follow the existing model style, provided the public contract is mutation-isolated.
