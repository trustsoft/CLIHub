## Context

`ConfigurationSnapshot` now isolates reads from the service-owned graph, but `IPreferencesStore.Load()` returns a mutable `AppPreferences` and `IProjectStateStore.Load()` returns a mutable `ProjectState`. Callers mutate those values and then call `Save`, which still makes persistence responsibility depend on caller discipline.

The next configuration step is to make stores the owners of update transactions. The existing snapshot remains the persistence boundary; this change narrows the store APIs around explicit update operations before schema versioning and repository work begin.

## Goals / Non-Goals

**Goals:**

- Make preference and project updates explicit at their owning stores.
- Ensure an update callback operates on a detached value and is persisted only when the operation completes successfully.
- Preserve unrelated configuration state and the current config writer behavior.
- Migrate all application and Core callers in one compile-checked change.

**Non-Goals:**

- Change the JSON schema or add schema migrations.
- Introduce `IConfigurationRepository` or debounce changes.
- Add optimistic concurrency/version conflict handling beyond the existing serialized config service.
- Change user-visible settings or project-management behavior.

## Decisions

- Replace `Load`/`Save` store pairs with explicit operations such as `Update(Action<AppPreferences>)` and `Update(Action<ProjectState>)`, or an equivalent repository-local naming that clearly makes the transaction boundary explicit.
- The store loads a detached snapshot, clones only its owned section into the callback value, applies the callback, and submits a new snapshot through `IConfigService` after the callback returns.
- If the callback throws, the store SHALL not persist a partial update; the exception propagates to the caller.
- Read-only callers receive detached values through `Get`/`Load`-style methods but have no store `Save` method to accidentally pair with them.
- Migrate callers that need to modify preferences to call one explicit update operation. Read-only callers continue using the read operation.
- Migrate project service persistence to an explicit project-state update operation; project service remains the owner of its in-memory mutable `ProjectState`.
- Keep compatibility changes internal to this repository; no adapter preserving the old public store methods is required.

## Risks / Trade-offs

- **Many callers require mechanical migration** -> Change interfaces first and use compiler errors to find every caller.
- **Callback mutation leaks into a shared object** -> Pass a detached copy and test reference isolation.
- **Callback throws after partial mutation** -> Persist only after successful callback completion and test that the stored snapshot is unchanged.

## Migration Plan

1. Update store interfaces and add explicit update methods.
2. Implement detached update transactions in both stores.
3. Migrate Core/application callers and test fakes.
4. Add explicit update, exception, and unrelated-state tests.
5. Run full build/test and validate/sync the configuration snapshot spec.
6. Rollback consists of restoring the previous store interfaces and caller migrations; config files remain compatible.

## Open Questions

None.
