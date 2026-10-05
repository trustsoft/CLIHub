## Why

The snapshot boundary prevents shared-reference mutation, but store APIs still expose mutable values through `Load()` and rely on callers to mutate them before `Save()`. This leaves update ownership implicit and makes it easy for future callers to forget persistence or accidentally mix unrelated state changes.

## What Changes

- Replace mutable store `Load/Save` usage with explicit update operations for preferences and project state.
- Provide update callbacks or equivalent commands that receive an isolated value and persist the returned update as one store operation.
- Migrate settings, release notes, agent services, project management, and test fakes/callers to the explicit APIs.
- Ensure each store updates only its owned configuration section while preserving unrelated sections.
- Add tests for explicit updates, no-op reads, failed/aborted updates, and preservation of unrelated state.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `configuration-snapshot`: Require explicit store update operations instead of caller-managed mutable `Load`/`Save` sequences.

## Impact

- Affected Core contracts and implementations: `IPreferencesStore`, `PreferencesStore`, `IProjectStateStore`, and `ProjectStateStore`.
- Affected application callers include settings, release notes, agent detection/version services, project service, and legacy UI paths that currently mutate loaded values.
- Existing `config.json` format and `IConfigService` persistence guarantees remain unchanged.
- This is a source-level API migration within the repository; no user data migration is required.
